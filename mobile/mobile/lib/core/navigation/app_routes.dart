import 'package:flutter/material.dart';

import '../../screens/ai/ai_chat_screen.dart';
import '../../screens/bookings/bookings_list.dart';
import '../../screens/debts/debts_list.dart';
import '../../screens/employees/employees_list.dart';
import '../../screens/expenses/expenses_list.dart';
import '../../screens/finance/finance_screen.dart';
import '../../screens/information/information_screen.dart';
import '../../screens/notes/notes_screen.dart';
import '../../screens/payments/payments_main_screen.dart';
import '../../screens/reports/reports_screen.dart';
import '../../screens/rooms/rooms_list.dart';
import '../../screens/security/blacklist_screen.dart';
import '../../screens/settings/settings_screen.dart';
import '../../screens/dashboard_screen.dart';

/// تعريف موحد لمسار الواجهة.
///
/// يجب أن يبقى هذا الملف مصدر الحقيقة للتنقل والصلاحيات الأساسية. أي مسار
/// فرعي خاص بميزة (مثل /payments/history) يمكن أن يعرّف داخل feature نفسها
/// دون إعادة إدراجه في القائمة الرئيسية.
class AppRouteDefinition {
  const AppRouteDefinition({
    required this.path,
    required this.title,
    required this.permission,
    required this.icon,
    required this.builder,
  });

  final String path;
  final String title;
  final String permission;
  final IconData icon;
  final WidgetBuilder builder;

  bool matches(String route) => route == path || route.startsWith('$path/');
}

final List<AppRouteDefinition> appRouteDefinitions = <AppRouteDefinition>[
  AppRouteDefinition(
    path: '/dashboard',
    title: 'لوحة التحكم',
    permission: 'dashboard',
    icon: Icons.dashboard,
    builder: (_) => const DashboardScreen(),
  ),
  AppRouteDefinition(
    path: '/rooms',
    title: 'إدارة الغرف',
    permission: 'rooms',
    icon: Icons.bed,
    builder: (_) => const RoomsListScreen(),
  ),
  AppRouteDefinition(
    path: '/bookings',
    title: 'إدارة الحجوزات',
    permission: 'bookings',
    icon: Icons.assignment,
    builder: (_) => const BookingsListScreen(),
  ),
  AppRouteDefinition(
    path: '/payments',
    title: 'إدارة المدفوعات',
    permission: 'payments',
    icon: Icons.attach_money,
    builder: (_) => const PaymentsMainScreen(),
  ),
  AppRouteDefinition(
    path: '/debts',
    title: 'الديون',
    permission: 'debts',
    icon: Icons.account_balance,
    builder: (_) => const DebtsListScreen(),
  ),
  AppRouteDefinition(
    path: '/expenses',
    title: 'إدارة المصروفات',
    permission: 'expenses',
    icon: Icons.receipt_long,
    builder: (_) => const ExpensesListScreen(),
  ),
  AppRouteDefinition(
    path: '/finance',
    title: 'الصندوق والمالية',
    permission: 'finance',
    icon: Icons.account_balance_wallet,
    builder: (_) => const FinanceScreen(),
  ),
  AppRouteDefinition(
    path: '/reports',
    title: 'التقارير',
    permission: 'reports',
    icon: Icons.bar_chart,
    builder: (_) => const ReportsScreen(),
  ),
  AppRouteDefinition(
    path: '/notes',
    title: 'الملاحظات والتنبيهات',
    permission: 'notes',
    icon: Icons.note,
    builder: (_) => const NotesScreen(),
  ),
  AppRouteDefinition(
    path: '/blacklist',
    title: 'القائمة السوداء',
    permission: 'settings',
    icon: Icons.gavel,
    builder: (_) => const BlacklistScreen(),
  ),
  AppRouteDefinition(
    path: '/information',
    title: 'سجل المعلومية',
    permission: 'information',
    icon: Icons.badge,
    builder: (_) => const InformationScreen(),
  ),
  AppRouteDefinition(
    path: '/ai',
    title: 'المساعد الذكي',
    permission: 'settings',
    icon: Icons.smart_toy,
    builder: (_) => const AiChatScreen(),
  ),
  AppRouteDefinition(
    path: '/settings',
    title: 'الإعدادات',
    permission: 'settings',
    icon: Icons.settings,
    builder: (_) => const SettingsScreen(),
  ),
];

AppRouteDefinition? appRouteFor(String path) {
  for (final route in appRouteDefinitions) {
    if (route.matches(path)) return route;
  }
  return null;
}
