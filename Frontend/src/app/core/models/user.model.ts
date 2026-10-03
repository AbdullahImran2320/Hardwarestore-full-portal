export interface AppUser {
  id: number;
  username: string;
  role: string;
  mustChangePassword: boolean;
}

export interface RegisterUser {
  username: string;
  password: string;
  role: string;
}

export interface ChangePassword {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}
