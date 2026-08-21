import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../core/navigation/app_routes.dart';
import '../providers/auth_provider.dart';

class AdminSidebar extends ConsumerWidget {
  const AdminSidebar({
    required this.currentRoute,
    required this.onRouteSelected,
    super.key,
  });
  final String currentRoute;
  final void Function(String) onRouteSelected;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final auth = ref.watch(authProvider);
    bool can(String key) {
      final u = auth.currentUser;
      if (u == null) {
        return false;
      }
      if (u.permissions.contains('all') || u.userType == 'admin') {
        return true;
      }
      return u.permissions.contains(key);
    }

    const sidebarColor = Color(0xFF0F172A);
    const headerColor = Color(0xFF16213C);
    final cardOverlay = Colors.white.withValues(alpha: 0.08);
    final dividerColor = Colors.white.withValues(alpha: 0.12);
    final inactiveColor = Colors.white.withValues(alpha: 0.72);

    return Container(
      width: 280,
      color: sidebarColor,
      child: Column(
        children: [
          Container(
            padding: const EdgeInsets.all(24),
            decoration: BoxDecoration(
              color: headerColor,
              border: Border(bottom: BorderSide(color: dividerColor)),
            ),
            child: Column(
              children: [
                // Logo section
                Row(
                  children: [
                    Container(
                      padding: const EdgeInsets.all(12),
                      decoration: BoxDecoration(
                        color: cardOverlay,
                        borderRadius: BorderRadius.circular(12),
                      ),
                      child: const Icon(
                        Icons.hotel,
                        color: Colors.white,
                        size: 32,
                      ),
                    ),
                    const SizedBox(width: 16),
                    const Expanded(
                      child: Text(
                        'فندق مارينا بلازا',
                        style: TextStyle(
                          color: Colors.white,
                          fontSize: 18,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 20),

                // User info section
                Container(
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: cardOverlay,
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: Row(
                    children: [
                      CircleAvatar(
                        backgroundColor: Colors.white.withValues(alpha: 0.2),
                        child: const Icon(Icons.person, color: Colors.white),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              auth.currentUser?.name ?? 'مستخدم',
                              style: const TextStyle(
                                color: Colors.white,
                                fontWeight: FontWeight.w600,
                                fontSize: 14,
                              ),
                            ),
                            Text(
                              auth.currentUser?.userType == 'admin'
                                  ? 'مدير النظام'
                                  : 'موظف',
                              style: TextStyle(
                                color: inactiveColor,
                                fontSize: 12,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),

          Expanded(
            child: ListView(
              padding: const EdgeInsets.symmetric(vertical: 16),
              children: [
                for (final item in appRouteDefinitions)
                  if (can(item.permission))
                    _buildMenuItem(
                      icon: item.icon,
                      title: item.title,
                      route: item.path,
                      isActive: item.matches(currentRoute),
                      onTap: () => onRouteSelected(item.path),
                      context: context,
                    ),
              ],
            ),
          ),
          Padding(
            padding: const EdgeInsets.all(12),
            child: _buildMenuItem(
              icon: Icons.logout,
              title: 'تسجيل الخروج',
              route: '/logout',
              isActive: false,
              onTap: () async {
                // إغلاق الـ Drawer في الموبايل قبل تسجيل الخروج
                try {
                  final isTablet = MediaQuery.of(context).size.width >= 768;
                  if (!isTablet && Navigator.of(context).canPop()) {
                    Navigator.of(context).pop();
                  }
                } catch (e) {
                  // تجاهل الأخطاء
                }

                await ref.read(authProvider.notifier).logout();
              },
              context: context,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildMenuItem({
    required IconData icon,
    required String title,
    required String route,
    required bool isActive,
    required VoidCallback onTap,
    BuildContext? context,
  }) {
    return Material(
      color: isActive
          ? Colors.white.withValues(alpha: 0.12)
          : Colors.transparent,
      borderRadius: BorderRadius.circular(8),
      child: ListTile(
        leading: Icon(
          icon,
          color: isActive ? Colors.white : Colors.white.withValues(alpha: 0.72),
        ),
        title: Text(
          title,
          style: TextStyle(
            color: isActive
                ? Colors.white
                : Colors.white.withValues(alpha: 0.72),
            fontWeight: isActive ? FontWeight.w600 : FontWeight.normal,
          ),
        ),
        onTap: () {
          // إغلاق الـ Drawer في الموبايل قبل التنقل
          if (context != null) {
            try {
              // تحقق مما إذا كان هناك drawer مفتوح وأغلقه
              final isTablet = MediaQuery.of(context).size.width >= 768;
              if (!isTablet && Navigator.of(context).canPop()) {
                Navigator.of(context).pop();
              }
            } catch (e) {
              // تجاهل الأخطاء ومتابع
            }
          }
          onTap();
        },
        contentPadding: const EdgeInsets.symmetric(horizontal: 16),
        dense: true,
      ),
    );
  }
}
