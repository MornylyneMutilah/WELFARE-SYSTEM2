export const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export type RegistrationFormValues = {
  firstName: string;
  lastName: string;
  email: string;
  employeeId: string;
  departmentId: number;
  password: string;
  confirmPassword: string;
  requestedRole: string;
  confirmed: boolean;
};

export type FieldErrors = Partial<Record<keyof RegistrationFormValues, string>>;

export function validateStep1(values: RegistrationFormValues): FieldErrors {
  const errors: FieldErrors = {};

  if (!values.firstName.trim()) errors.firstName = 'First name is required.';
  if (!values.lastName.trim()) errors.lastName = 'Last name is required.';
  if (!values.email.trim()) {
    errors.email = 'Email address is required.';
  } else if (!EMAIL_PATTERN.test(values.email.trim())) {
    errors.email = 'Enter a valid email address.';
  }
  if (!values.employeeId.trim()) errors.employeeId = 'Employee ID is required.';
  if (!values.departmentId || values.departmentId < 1) {
    errors.departmentId = 'Select a department.';
  }
  if (!values.password) {
    errors.password = 'Password is required.';
  } else if (values.password.length < 8) {
    errors.password = 'Password must be at least 8 characters.';
  }
  if (!values.confirmPassword) {
    errors.confirmPassword = 'Confirm your password.';
  } else if (values.password !== values.confirmPassword) {
    errors.confirmPassword = 'Passwords do not match.';
  }

  return errors;
}

export function validateStep2(values: RegistrationFormValues): FieldErrors {
  const allowed = ['Employee', 'HROfficer', 'CommitteeMember', 'Management', 'Administrator'];
  if (!allowed.includes(values.requestedRole)) {
    return { requestedRole: 'Select an account type.' };
  }
  return {};
}

export function validateStep3(values: RegistrationFormValues): FieldErrors {
  if (!values.confirmed) {
    return {
      confirmed:
        'You must confirm that the information is accurate and that you agree to the terms and privacy policy.',
    };
  }
  return {};
}

export function reviewSummary(values: RegistrationFormValues, departmentName?: string) {
  return {
    fullName: `${values.firstName.trim()} ${values.lastName.trim()}`.trim(),
    email: values.email.trim(),
    employeeId: values.employeeId.trim(),
    department: departmentName || `Department #${values.departmentId}`,
    requestedRole: values.requestedRole,
  };
}
