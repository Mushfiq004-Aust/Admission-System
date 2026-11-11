import { useMemo, useState } from "react";
import { ErrorMessage } from "../components/ErrorMessage";
import { LoadingState } from "../components/LoadingState";
import { useNavigate } from "react-router-dom";
import { ClassCard } from "../components/ClassCard";
import { useStudent } from "../context/StudentContext";
import { usePolling } from "../hooks/usePolling";
import { ApiError, api, messageOf } from "../services/api";
import type { AdmissionClass } from "../types";

const REFRESH_MS = 4000;
const PAGE_SIZE = 20;
export function ClassList() {
  const { studentId } = useStudent();
  const navigate = useNavigate();
  const { data: classes, error, loading, reload } = usePolling(() => api.listClasses(), REFRESH_MS, "classes");
  const [applyingId, setApplyingId] = useState<string | null>(null);
  const [applyError, setApplyError] = useState<{ classId: string; message: string } | null>(null);
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const filtered = useMemo(() => (classes ?? []).filter(c => c.name.toLowerCase().includes(search.trim().toLowerCase())), [classes, search]);
  const pages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const visible = filtered.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);
  const apply = async (cls: AdmissionClass) => {
    if (!studentId) return;
    setApplyingId(cls.id); setApplyError(null);
    try { const hold = await api.apply(cls.id, studentId); navigate(`/applications/${hold.holdId}`); }
    catch (e) { setApplyError({ classId: cls.id, message: messageOf(e) }); if (e instanceof ApiError && e.status === 409) void reload(); }
    finally { setApplyingId(null); }
  };
  return <section>
    <div className="hero"><p className="eyebrow">SCHOOL ADMISSIONS</p><h1>Find your class</h1><p className="lead">Choose a class and shift, then temporarily hold a seat for your child. Seat availability updates automatically.</p><ol className="steps"><li><strong>1</strong> Enter the birth registration number</li><li><strong>2</strong> Press Apply to hold a seat</li><li><strong>3</strong> Confirm before the timer ends</li></ol></div>
    {!studentId && <p className="alert alert-info">Enter the student's birth registration number at the top of the page to apply.</p>}
    {error && <ErrorMessage message={error} />}{loading && !classes && <LoadingState text="Loading classes…" />}
    {classes && classes.length > 0 && <div className="class-list-toolbar"><div><h2>Available classes</h2><p className="muted">{filtered.length} {filtered.length === 1 ? "class" : "classes"} found</p></div><input aria-label="Search classes" value={search} onChange={e => {setSearch(e.target.value);setPage(1);}} placeholder="Search class or shift…" /></div>}
    {classes && classes.length === 0 && <p className="empty">No classes are open for admission yet. Please check back soon.</p>}
    {classes && classes.length > 0 && filtered.length === 0 && <p className="empty">No classes match “{search}”. Try another search.</p>}
    <div className="grid">{visible.map(cls => <ClassCard key={cls.id} cls={cls} busy={applyingId === cls.id} disabledReason={studentId ? null : "Enter a birth registration number first"} error={applyError?.classId === cls.id ? applyError.message : null} onApply={apply} />)}</div>
    {filtered.length > 0 && <div className="pagination"><span>Showing {(page-1)*PAGE_SIZE+1}–{Math.min(page*PAGE_SIZE,filtered.length)} of {filtered.length}</span><div><button className="btn btn-ghost" disabled={page===1} onClick={() => setPage(p=>p-1)}>Previous</button><span className="page-count">Page {page} of {pages}</span><button className="btn btn-ghost" disabled={page===pages} onClick={() => setPage(p=>p+1)}>Next</button></div></div>}
  </section>;
}
