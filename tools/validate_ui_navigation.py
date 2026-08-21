from __future__ import annotations

import re
from pathlib import Path

APP = Path('/home/ubuntu/oraxhotel2024/mobile/mobile')
ROUTES = APP / 'lib/core/navigation/app_routes.dart'
MAIN = APP / 'lib/main.dart'
SIDEBAR = APP / 'lib/components/admin_sidebar.dart'


def main() -> None:
    route_text = ROUTES.read_text(encoding='utf-8')
    paths = re.findall(r"path: '([^']+)'", route_text)
    assert paths, 'No centralized routes found'
    assert len(paths) == len(set(paths)), f'Duplicate routes: {paths}'
    assert len(paths) == 13, f'Unexpected route count: {len(paths)}'
    main_text = MAIN.read_text(encoding='utf-8')
    assert 'class HomeShell' not in main_text
    assert 'class RootRouter' not in main_text
    assert "import 'core/navigation/root_router.dart';" in main_text
    sidebar_text = SIDEBAR.read_text(encoding='utf-8')
    assert 'appRouteDefinitions' in sidebar_text
    assert "route: '/dashboard'" not in sidebar_text
    payment_shell = (APP / 'lib/screens/payments/widgets/payments_main_shell.dart').read_text(encoding='utf-8')
    payment_screen = (APP / 'lib/screens/payments/payments_main_screen.dart').read_text(encoding='utf-8')
    assert 'class PaymentsMainShell' in payment_shell
    assert 'PaymentsMainShell(' in payment_screen
    assert 'TabController' not in payment_screen
    print({'route_count': len(paths), 'routes_unique': True, 'shell_extracted': True, 'payments_tabs_extracted': True})


if __name__ == '__main__':
    main()
