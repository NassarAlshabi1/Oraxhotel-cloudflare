import type { Database } from './database';
import type { AuthContext } from './auth';

const MAX_COMMANDS = 100;
const MAX_PAYLOAD_SIZE = 5 * 1024 * 1024;

interface DesktopCommandInput {
  command_id?: unknown;
  idempotency_key?: unknown;
  entity?: unknown;
  operation?: unknown;
  local_uuid?: unknown;
  payload?: unknown;
  vector_clock?: unknown;
  requested_at?: unknown;
  requested_by?: unknown;
}

function json(data: unknown, status = 200): Response {
  return new Response(JSON.stringify(data), {
    status,
    headers: {
      'Content-Type': 'application/json',
      'Access-Control-Allow-Origin': '*',
      'Access-Control-Allow-Methods': 'GET, POST, OPTIONS',
      'Access-Control-Allow-Headers': 'Authorization, Content-Type, X-Device-Id',
    },
  });
}

function text(value: unknown): string | null {
  return typeof value === 'string' && value.trim().length > 0 ? value.trim() : null;
}

/**
 * Accepts mobile changes as commands only. The desktop publisher is the
 * component that applies them to SQL Server and publishes the canonical row.
 */
export async function handleDesktopCommands(
  request: Request,
  db: Database,
  _ctx: AuthContext,
): Promise<Response> {
  try {
    const contentLength = Number(request.headers.get('Content-Length') || 0);
    if (contentLength > MAX_PAYLOAD_SIZE) return json({ error: 'Payload too large' }, 413);

    const body = await request.json() as { commands?: DesktopCommandInput[] };
    if (!Array.isArray(body.commands)) return json({ error: 'commands array required' }, 400);
    if (body.commands.length > MAX_COMMANDS) return json({ error: `Max ${MAX_COMMANDS} commands per batch` }, 400);

    const results: Array<Record<string, unknown>> = [];
    for (const command of body.commands) {
      const commandId = text(command.command_id);
      const idempotencyKey = text(command.idempotency_key);
      const entity = text(command.entity);
      const operation = text(command.operation);
      const localUuid = text(command.local_uuid);
      if (!commandId || !idempotencyKey || !entity || !operation || !localUuid) {
        results.push({ idempotency_key: idempotencyKey || 'unknown', success: false, error: 'command_id, idempotency_key, entity, operation and local_uuid are required' });
        continue;
      }
      if (!['create', 'update', 'delete'].includes(operation)) {
        results.push({ idempotency_key: idempotencyKey, success: false, error: 'Invalid operation' });
        continue;
      }
      if (!['rooms', 'bookings', 'payments', 'expenses', 'employees', 'debts', 'booking_notes', 'shift_notes', 'cash_transactions', 'booking_nights', 'salary_cycles', 'salary_payments', 'salary_withdrawals', 'salary_carry_over_logs', 'price_adjustments', 'booking_price_adjustments', 'audit_logs', 'payment_voids', 'guest_infos'].includes(entity)) {
        results.push({ idempotency_key: idempotencyKey, success: false, error: `Unsupported desktop projection entity: ${entity}` });
        continue;
      }
      if (command.payload === null || typeof command.payload !== 'object') {
        results.push({ idempotency_key: idempotencyKey, success: false, error: 'payload object is required' });
        continue;
      }

      const payloadJson = JSON.stringify(command.payload);
      const vectorClock = text(command.vector_clock) || '{}';
      const requestedAt = typeof command.requested_at === 'number' && Number.isFinite(command.requested_at)
        ? Math.max(0, Math.floor(command.requested_at))
        : Math.floor(Date.now() / 1000);
      const requestedBy = text(command.requested_by);

      await db.raw.prepare(
        'INSERT OR IGNORE INTO desktop_sync_commands (command_id, idempotency_key, entity, operation, local_uuid, payload_json, vector_clock, requested_at, requested_by, status) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, \'pending\')'
      ).bind(
        commandId,
        idempotencyKey,
        entity,
        operation,
        localUuid,
        payloadJson,
        vectorClock,
        requestedAt,
        requestedBy,
      ).run();

      const stored = await db.raw.prepare(
        'SELECT command_id, idempotency_key, status, result_json, error FROM desktop_sync_commands WHERE idempotency_key = ?'
      ).bind(idempotencyKey).first<{ command_id: string; idempotency_key: string; status: string; result_json: string | null; error: string | null }>();

      results.push({
        idempotency_key: idempotencyKey,
        command_id: stored?.command_id || commandId,
        success: stored?.status !== 'rejected',
        accepted: stored?.status === 'pending',
        status: stored?.status || 'pending',
        error: stored?.error || undefined,
      });
    }

    const success = results.filter(result => result.success === true).length;
    return json({
      results,
      summary: { total: results.length, success, failed: results.length - success },
      server_time: Math.floor(Date.now() / 1000),
    });
  } catch (error) {
    console.error('[DESKTOP/COMMANDS] Error:', error);
    return json({ error: 'Desktop command enqueue failed', detail: String(error) }, 500);
  }
}
