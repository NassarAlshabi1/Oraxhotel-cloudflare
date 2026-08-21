import 'dart:async';
import 'dart:convert';
import 'dart:io' show SocketException;

import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

import 'daos/outbox_dao.dart';

/// Contract for applying a canonical desktop event to a local Drift table.
///
/// The adapter is deliberately table-specific: the desktop SQL Server schema
/// is the source of truth, so a generic field-name guess is not acceptable.
abstract interface class DesktopEntityAdapter {
  String get entity;

  /// Apply a canonical create/update event. Implementations must match by
  /// local_uuid/server_id according to the approved desktop mapping.
  Future<void> upsertCanonical(Map<String, dynamic> record);

  /// Apply a canonical soft-delete event.
  Future<void> deleteCanonical(Map<String, dynamic> record);
}

class DesktopFirstSyncConfig {
  const DesktopFirstSyncConfig({
    required this.workerUrl,
    required this.username,
    required this.password,
    required this.deviceId,
    this.commandPath = '/api/desktop/commands',
    this.pullPath = '/api/sync/pull',
    this.batchSize = 25,
    this.pullPageSize = 100,
    this.requestTimeout = const Duration(seconds: 30),
    this.cursorPreferenceKey = 'desktop_first_sync_cursor',
  });

  final String workerUrl;
  final String username;
  final String password;
  final String deviceId;

  /// Mobile changes are commands for the desktop source of truth.
  final String commandPath;

  /// Canonical desktop projections are pulled after the desktop publisher
  /// has accepted and published them to D1.
  final String pullPath;

  final int batchSize;
  final int pullPageSize;
  final Duration requestTimeout;
  final String cursorPreferenceKey;
}

enum DesktopSyncStatus { success, offlineQueued, partial, failed }

class DesktopSyncResult {
  const DesktopSyncResult({
    required this.status,
    this.pushed = 0,
    this.pulled = 0,
    this.queued = 0,
    this.failed = 0,
    this.conflicts = 0,
    this.error,
  });

  final DesktopSyncStatus status;
  final int pushed;
  final int pulled;
  final int queued;
  final int failed;
  final int conflicts;
  final String? error;
}

class DesktopSyncTransportException implements Exception {
  const DesktopSyncTransportException(this.message, {this.statusCode});

  final String message;
  final int? statusCode;

  bool get isTransient =>
      statusCode == null || statusCode! >= 500 || statusCode == 408 || statusCode == 429;

  @override
  String toString() =>
      'DesktopSyncTransportException(status=$statusCode, message=$message)';
}

class DesktopFirstSyncEngine {
  DesktopFirstSyncEngine({
    required DesktopFirstSyncConfig config,
    required OutboxDao outbox,
    required Map<String, DesktopEntityAdapter> adapters,
    http.Client? client,
    SharedPreferences? preferences,
  })  : _config = config,
        _outbox = outbox,
        _adapters = adapters,
        _client = client ?? http.Client(),
        _preferencesFuture = preferences == null
            ? SharedPreferences.getInstance()
            : Future<SharedPreferences>.value(preferences);

  final DesktopFirstSyncConfig _config;
  final OutboxDao _outbox;
  final Map<String, DesktopEntityAdapter> _adapters;
  final http.Client _client;
  final Future<SharedPreferences> _preferencesFuture;

  String? _token;
  bool _running = false;

  bool get isAuthenticated => _token != null;

  /// The UI writes to Drift and calls this method without waiting for network.
  /// The local write is already durable; the outbox entry is the durable
  /// delivery record. A failed network attempt never deletes local data.
  Future<int> enqueueLocalChange({
    required String entity,
    required String operation,
    required String localUuid,
    required Map<String, dynamic> payload,
    required int clientTimestamp,
    int? serverId,
  }) {
    if (!const {'create', 'update', 'delete'}.contains(operation)) {
      throw ArgumentError.value(operation, 'operation', 'unsupported operation');
    }
    return _outbox.merge(
      entity: entity,
      op: operation,
      localUuid: localUuid,
      payload: payload,
      clientTs: clientTimestamp,
      serverId: serverId,
      source: 'local',
    );
  }

  Future<DesktopSyncResult> sync({bool push = true, bool pull = true}) async {
    if (_running) {
      return const DesktopSyncResult(
        status: DesktopSyncStatus.partial,
        error: 'A sync operation is already running',
      );
    }
    _running = true;
    try {
      var pushed = 0;
      var failed = 0;
      var conflicts = 0;
      var pulled = 0;

      try {
        await _ensureToken();
      } on DesktopSyncTransportException catch (error) {
        if (error.isTransient) {
          return const DesktopSyncResult(status: DesktopSyncStatus.offlineQueued);
        }
        return DesktopSyncResult(
          status: DesktopSyncStatus.failed,
          error: error.message,
        );
      } on SocketException {
        return const DesktopSyncResult(status: DesktopSyncStatus.offlineQueued);
      } on TimeoutException {
        return const DesktopSyncResult(status: DesktopSyncStatus.offlineQueued);
      }

      if (push) {
        final pushResult = await _pushOutbox();
        pushed = pushResult.pushed;
        failed = pushResult.failed;
        conflicts = pushResult.conflicts;
      }
      if (pull && failed == 0) {
        pulled = await _pullCanonicalChanges();
      }

      if (failed > 0) {
        return DesktopSyncResult(
          status: pulled > 0
              ? DesktopSyncStatus.partial
              : DesktopSyncStatus.failed,
          pushed: pushed,
          pulled: pulled,
          failed: failed,
          conflicts: conflicts,
        );
      }
      return DesktopSyncResult(
        status: DesktopSyncStatus.success,
        pushed: pushed,
        pulled: pulled,
        conflicts: conflicts,
      );
    } on SocketException {
      return const DesktopSyncResult(status: DesktopSyncStatus.offlineQueued);
    } on TimeoutException {
      return const DesktopSyncResult(status: DesktopSyncStatus.offlineQueued);
    } on DesktopSyncTransportException catch (error) {
      return DesktopSyncResult(
        status: error.isTransient
            ? DesktopSyncStatus.offlineQueued
            : DesktopSyncStatus.failed,
        error: error.message,
      );
    } finally {
      _running = false;
    }
  }

  Future<void> _ensureToken() async {
    if (_token != null) return;
    final response = await _client
        .post(
          _uri('/api/auth/login'),
          headers: const {'Content-Type': 'application/json'},
          body: jsonEncode({
            'username': _config.username,
            'password': _config.password,
            'device_id': _config.deviceId,
          }),
        )
        .timeout(_config.requestTimeout);
    final body = _decodeObject(response.body);
    if (response.statusCode != 200 || body['token'] is! String) {
      throw DesktopSyncTransportException(
        _errorMessage(body, 'Login failed'),
        statusCode: response.statusCode,
      );
    }
    _token = body['token'] as String;
  }

  Future<_PushResult> _pushOutbox() async {
    await _outbox.reclaimForPush();
    final batch = await _outbox.takeBatch(
      _config.batchSize,
      sources: const ['local'],
    );
    if (batch.isEmpty) return const _PushResult();

    final commands = <Map<String, dynamic>>[];
    for (final item in batch) {
      commands.add({
        'command_id': 'flutter-${item.idempotencyKey ?? item.id}',
        'idempotency_key': item.idempotencyKey ?? 'outbox:${item.id}',
        'entity': item.entity,
        'operation': item.op,
        'local_uuid': item.localUuid,
        'payload': _decodePayload(item.payload),
        'vector_clock': _readVectorClock(item.payload),
        'requested_at': item.clientTs,
        'requested_by': _config.deviceId,
      });
    }

    try {
      final response = await _client
          .post(
            _uri(_config.commandPath),
            headers: _headers(contentType: 'application/json'),
            body: jsonEncode({'commands': commands}),
          )
          .timeout(_config.requestTimeout);
      final body = _decodeObject(response.body);
      if (response.statusCode == 401) {
        _token = null;
      }
      if (response.statusCode < 200 || response.statusCode >= 300) {
        final error = DesktopSyncTransportException(
          _errorMessage(body, 'Command push failed'),
          statusCode: response.statusCode,
        );
        await _markBatchError(
          batch,
          error.message,
          permanent: _isPermanentCommandError(response.statusCode),
        );
        if (error.isTransient) rethrow;
        return _PushResult(failed: batch.length);
      }

      final results = body['results'];
      if (results is! List) {
        final error = const DesktopSyncTransportException('Invalid command response');
        await _markBatchError(batch, error.message, permanent: false);
        throw error;
      }
      final byKey = <String, Map<String, dynamic>>{};
      for (final raw in results) {
        if (raw is Map) {
          final result = Map<String, dynamic>.from(raw);
          final key = result['idempotency_key'];
          if (key is String) byKey[key] = result;
        }
      }

      var pushed = 0;
      var failed = 0;
      var conflicts = 0;
      for (final item in batch) {
        final key = item.idempotencyKey ?? 'outbox:${item.id}';
        final result = byKey[key];
        if (result != null && result['success'] == true) {
          await _outbox.markDeliveredToPrimary(item.id);
          pushed++;
          if (result['conflict'] == true) conflicts++;
        } else {
          failed++;
          await _outbox.setDead(
            item.id,
            result?['error']?.toString() ?? 'Command rejected by desktop queue',
            item.attempts + 1,
          );
        }
      }
      return _PushResult(
        pushed: pushed,
        failed: failed,
        conflicts: conflicts,
      );
    } on SocketException {
      await _markBatchError(batch, 'Network unavailable', permanent: false);
      rethrow;
    } on TimeoutException {
      await _markBatchError(batch, 'Request timed out', permanent: false);
      rethrow;
    }
  }

  Future<int> _pullCanonicalChanges() async {
    final prefs = await _preferencesFuture;
    var cursor = prefs.getInt(_config.cursorPreferenceKey) ?? 0;
    var total = 0;
    var hasMore = true;

    while (hasMore) {
      final response = await _client
          .get(
            _uri('${_config.pullPath}?cursor=$cursor&limit=${_config.pullPageSize}'),
            headers: _headers(),
          )
          .timeout(_config.requestTimeout);
      final body = _decodeObject(response.body);
      if (response.statusCode == 401) _token = null;
      if (response.statusCode < 200 || response.statusCode >= 300) {
        throw DesktopSyncTransportException(
          _errorMessage(body, 'Canonical pull failed'),
          statusCode: response.statusCode,
        );
      }
      final rawChanges = body['changes'];
      if (rawChanges is! List) {
        throw const DesktopSyncTransportException('Invalid canonical pull response');
      }
      for (final raw in rawChanges) {
        if (raw is! Map) {
          throw const DesktopSyncTransportException('Invalid canonical change');
        }
        final change = Map<String, dynamic>.from(raw);
        final entity = change['_entity']?.toString() ?? change['entity']?.toString();
        if (entity == null) {
          throw const DesktopSyncTransportException('Canonical change has no entity');
        }
        final adapter = _adapters[entity];
        if (adapter == null) {
          throw DesktopSyncTransportException('No adapter registered for $entity');
        }
        final deletedAt = change['deleted_at'];
        if (deletedAt != null) {
          await adapter.deleteCanonical(change);
        } else {
          await adapter.upsertCanonical(change);
        }
        total++;
      }

      final responseCursor = body['cursor'];
      final nextCursor = responseCursor is int
          ? responseCursor
          : int.tryParse(responseCursor?.toString() ?? '') ?? cursor;
      hasMore = body['has_more'] == true;
      if (nextCursor < cursor) {
        throw const DesktopSyncTransportException('Server cursor moved backwards');
      }
      cursor = nextCursor;
      // Advance only after every record in this page was applied locally.
      await prefs.setInt(_config.cursorPreferenceKey, cursor);
    }
    return total;
  }

  Future<void> _markBatchError(
    List<OutboxData> batch,
    String message, {
    required bool permanent,
  }) async {
    for (final item in batch) {
      if (permanent) {
        await _outbox.setDead(item.id, message, item.attempts + 1);
      } else {
        await _outbox.setError(item.id, message, item.attempts + 1);
      }
    }
  }

  Uri _uri(String path) {
    final base = _config.workerUrl.endsWith('/')
        ? _config.workerUrl.substring(0, _config.workerUrl.length - 1)
        : _config.workerUrl;
    return Uri.parse('$base$path');
  }

  Map<String, String> _headers({String? contentType}) => {
        'Authorization': 'Bearer ${_token ?? ''}',
        if (contentType != null) 'Content-Type': contentType,
        'X-Device-Id': _config.deviceId,
      };

  static Map<String, dynamic> _decodeObject(String text) {
    final value = jsonDecode(text);
    if (value is! Map) throw const FormatException('Expected JSON object');
    return Map<String, dynamic>.from(value);
  }

  static Map<String, dynamic> _decodePayload(String payload) {
    final value = jsonDecode(payload);
    if (value is! Map) throw const FormatException('Outbox payload must be an object');
    return Map<String, dynamic>.from(value);
  }

  static String _readVectorClock(String payload) {
    final decoded = _decodePayload(payload);
    final value = decoded['vector_clock'] ?? decoded['vectorClock'];
    return value is String ? value : '{}';
  }

  static bool _isPermanentCommandError(int statusCode) =>
      statusCode == 400 || statusCode == 404 || statusCode == 409 || statusCode == 422;

  static String _errorMessage(Map<String, dynamic> body, String fallback) =>
      body['error']?.toString() ?? body['message']?.toString() ?? fallback;

  @override
  String toString() => 'DesktopFirstSyncEngine(device=${_config.deviceId})';

  void dispose() => _client.close();
}

class _PushResult {
  const _PushResult({this.pushed = 0, this.failed = 0, this.conflicts = 0});

  final int pushed;
  final int failed;
  final int conflicts;
}
