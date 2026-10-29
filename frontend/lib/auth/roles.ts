export type AppRole =
  | 'Employee'
  | 'HROfficer'
  | 'CommitteeMember'
  | 'Management'
  | 'Administrator';

export const ROLE_LABELS: Record<AppRole, string> = {
  Employee: 'Employee',
  HROfficer: 'HR Officer',
  CommitteeMember: 'Welfare Committee Member',
  Management: 'Institutional Management',
  Administrator: 'Administrator',
};

export const PRIVILEGED_ROLES: AppRole[] = [
  'HROfficer',
  'CommitteeMember',
  'Management',
  'Administrator',
];

export function isAppRole(value: string | undefined | null): value is AppRole {
  return (
    value === 'Employee' ||
    value === 'HROfficer' ||
    value === 'CommitteeMember' ||
    value === 'Management' ||
    value === 'Administrator'
  );
}

export function dashboardPathForRole(role: string | undefined | null): string {
  switch (role) {
    case 'HROfficer':
      return '/hr/dashboard';
    case 'CommitteeMember':
      return '/committee/dashboard';
    case 'Management':
      return '/management/dashboard';
    case 'Administrator':
      return '/admin/dashboard';
    default:
      return '/employee/dashboard';
  }
}

export type NavItem = {
  href: string;
  label: string;
};

export function navigationForRole(role: string | undefined | null): NavItem[] {
  switch (role) {
    case 'HROfficer':
      return [
        { href: '/hr/dashboard', label: 'Dashboard' },
        { href: '/hr/reviews', label: 'Reviews' },
        { href: '/employee/programs', label: 'Programs' },
        { href: '/employee/profile', label: 'Profile' },
      ];
    case 'CommitteeMember':
      return [
        { href: '/committee/dashboard', label: 'Dashboard' },
        { href: '/hr/reviews', label: 'Reviews' },
        { href: '/employee/programs', label: 'Programs' },
        { href: '/employee/profile', label: 'Profile' },
      ];
    case 'Management':
      return [
        { href: '/management/dashboard', label: 'Dashboard' },
        { href: '/hr/reviews', label: 'Approvals' },
        { href: '/employee/programs', label: 'Programs' },
        { href: '/employee/profile', label: 'Profile' },
      ];
    case 'Administrator':
      return [
        { href: '/admin/dashboard', label: 'Dashboard' },
        { href: '/admin/users', label: 'Users' },
        { href: '/admin/role-requests', label: 'Role requests' },
        { href: '/employee/programs', label: 'Programs' },
        { href: '/employee/profile', label: 'Profile' },
      ];
    default:
      return [
        { href: '/employee/dashboard', label: 'Dashboard' },
        { href: '/employee/programs', label: 'Programs' },
        { href: '/employee/applications', label: 'Applications' },
        { href: '/employee/applications/new', label: 'New application' },
        { href: '/employee/profile', label: 'Profile' },
      ];
  }
}
