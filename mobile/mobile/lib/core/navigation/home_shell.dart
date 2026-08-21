import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../components/admin_layout.dart';
import '../../providers/auth_provider.dart';
import '../../providers/repository_providers.dart';
import '../../screens/notes/notes_screen.dart' deferred as notes;
import 'app_routes.dart';

class HomeShell extends ConsumerStatefulWidget {
  const HomeShell({super.key});

  @override
  ConsumerState<HomeShell> createState() => _HomeShellState();
}

class _HomeShellState extends ConsumerState<HomeShell> {
  String _currentRoute = '/dashboard';

  Widget _buildRoute(String route) {
    final definition = appRouteFor(route);
    return definition?.builder(context) ?? appRouteDefinitions.first.builder(context);
  }

  bool _can(String permission) {
    final user = ref.read(authProvider).currentUser;
    if (user == null) return false;
    if (user.userType == 'admin' || user.permissions.contains('all')) return true;
    return user.permissions.contains(permission);
  }

  @override
  Widget build(BuildContext context) {
    final route = appRouteFor(_currentRoute);
    final allowed = route != null && _can(route.permission);
    final body = allowed
        ? _buildRoute(_currentRoute)
        : const Center(child: Text('ليس لديك صلاحية لعرض هذه الصفحة'));

    return AdminLayout(
      currentRoute: _currentRoute,
      body: body,
      actions: _buildGlobalActions(context),
      onRouteSelected: _navigateToRoute,
    );
  }

  List<Widget> _buildGlobalActions(BuildContext context) {
    final unreadCountAsync = ref.watch(simpleNotesUnreadCountProvider);
    final unreadCount = unreadCountAsync.maybeWhen(
      data: (count) => count,
      orElse: () => 0,
    );
    final hasUnread = unreadCount > 0;

    return [
      IconButton(
        onPressed: () async {
          await notes.loadLibrary();
          if (!context.mounted) return;
          await Navigator.of(context).push<void>(
            MaterialPageRoute<void>(builder: (_) => notes.NotesScreen()),
          );
        },
        tooltip: 'التنبيهات',
        icon: Stack(
          clipBehavior: Clip.none,
          children: [
            Icon(
              hasUnread ? Icons.notifications_active : Icons.notifications_none,
            ),
            if (hasUnread)
              Positioned(
                right: -2,
                top: -2,
                child: Container(
                  padding: const EdgeInsets.all(4),
                  decoration: const BoxDecoration(
                    color: Colors.red,
                    shape: BoxShape.circle,
                  ),
                  constraints: const BoxConstraints(minWidth: 16, minHeight: 16),
                  child: Text(
                    unreadCount > 9 ? '9+' : '$unreadCount',
                    style: const TextStyle(
                      color: Colors.white,
                      fontSize: 10,
                      fontWeight: FontWeight.bold,
                    ),
                    textAlign: TextAlign.center,
                  ),
                ),
              ),
          ],
        ),
      ),
    ];
  }

  void _navigateToRoute(String route) {
    if (appRouteFor(route) == null) return;
    setState(() => _currentRoute = route);
  }
}
