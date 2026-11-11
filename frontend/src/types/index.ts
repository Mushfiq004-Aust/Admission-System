export type HoldStatus = "HELD" | "CONFIRMED" | "EXPIRED" | "CANCELLED";

export interface AdmissionClass {
  id: string;
  name: string;
  totalSeats: number;
  heldSeats: number;
  confirmedSeats: number;
  availableSeats: number;
}

export interface Hold {
  holdId: string;
  classId: string;
  className: string;
  studentId: string;
  status: HoldStatus;
  expiresAt: string;
  createdAt: string;
  confirmedAt?: string | null;
  serverTime?: string;
}

export interface CreatedHold {
  holdId: string;
  classId: string;
  className?: string;
  studentId: string;
  status: HoldStatus;
  expiresAt: string;
  serverTime?: string;
}

export interface ConfirmResult {
  holdId: string;
  status: HoldStatus;
}

export interface AdminSession {
  token: string;
  username: string;
  expiresAt: string;
}
