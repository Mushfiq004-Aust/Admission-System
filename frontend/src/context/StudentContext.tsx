import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from "react";

const STORAGE_KEY = "school-admission.studentId";

export const isValidStudentId = (value: string): boolean => /^[0-9]{17}$/.test(value);

interface StudentContextValue {
  studentId: string | null;
  saveStudentId: (id: string) => void;
  clearStudentId: () => void;
}

const StudentContext = createContext<StudentContextValue | null>(null);

function readStored(): string | null {
  try {
    const v = localStorage.getItem(STORAGE_KEY);
    return v && isValidStudentId(v) ? v : null;
  } catch {
    return null;
  }
}

export function StudentProvider({ children }: { children: ReactNode }) {
  const [studentId, setStudentId] = useState<string | null>(readStored);

  const saveStudentId = useCallback((id: string) => {
    setStudentId(id);
    try {
      localStorage.setItem(STORAGE_KEY, id);
    } catch {
      /* storage unavailable: identity just won't survive a refresh */
    }
  }, []);

  const clearStudentId = useCallback(() => {
    setStudentId(null);
    try {
      localStorage.removeItem(STORAGE_KEY);
    } catch {
      /* ignore */
    }
  }, []);

  const value = useMemo(() => ({ studentId, saveStudentId, clearStudentId }), [studentId, saveStudentId, clearStudentId]);
  return <StudentContext.Provider value={value}>{children}</StudentContext.Provider>;
}

export function useStudent(): StudentContextValue {
  const ctx = useContext(StudentContext);
  if (!ctx) throw new Error("useStudent must be used inside <StudentProvider>");
  return ctx;
}
