import 'package:flutter/material.dart';

import '../../../components/app_scaffold.dart';

class PaymentsMainShell extends StatefulWidget {
  const PaymentsMainShell({
    required this.overviewBuilder,
    required this.transactionsBuilder,
    required this.activeBookingsBuilder,
    required this.onNewPayment,
    required this.isSaving,
    super.key,
  });

  final WidgetBuilder overviewBuilder;
  final WidgetBuilder transactionsBuilder;
  final WidgetBuilder activeBookingsBuilder;
  final VoidCallback onNewPayment;
  final ValueListenable<bool> isSaving;

  @override
  State<PaymentsMainShell> createState() => _PaymentsMainShellState();
}

class _PaymentsMainShellState extends State<PaymentsMainShell>
    with SingleTickerProviderStateMixin {
  late final TabController _tabs;

  @override
  void initState() {
    super.initState();
    _tabs = TabController(length: 3, vsync: this);
  }

  @override
  void dispose() {
    _tabs.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AppScaffold(
      title: 'إدارة المدفوعات',
      fab: ValueListenableBuilder<bool>(
        valueListenable: widget.isSaving,
        builder: (context, isSaving, _) {
          return FloatingActionButton.extended(
            onPressed: isSaving ? null : widget.onNewPayment,
            icon: const Icon(Icons.add_card),
            label: const Text('دفعة جديدة'),
            backgroundColor: Colors.green,
          );
        },
      ),
      body: Column(
        children: [
          TabBar(
            controller: _tabs,
            labelColor: Colors.green.shade800,
            unselectedLabelColor: Colors.grey.shade600,
            indicatorColor: Colors.green,
            labelStyle: const TextStyle(
              fontSize: 12,
              fontWeight: FontWeight.bold,
            ),
            unselectedLabelStyle: const TextStyle(fontSize: 11),
            tabs: const [
              Tab(text: 'نظرة عامة', icon: Icon(Icons.dashboard, size: 18)),
              Tab(text: 'المعاملات', icon: Icon(Icons.list, size: 18)),
              Tab(text: 'الحجوزات النشطة', icon: Icon(Icons.hotel, size: 18)),
            ],
          ),
          Expanded(
            child: TabBarView(
              controller: _tabs,
              children: [
                Builder(builder: widget.overviewBuilder),
                Builder(builder: widget.transactionsBuilder),
                Builder(builder: widget.activeBookingsBuilder),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
