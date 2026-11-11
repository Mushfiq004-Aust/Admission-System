import { useMemo, useState } from "react";
import { StatusBadge } from "../../components/StatusBadge";
import type { AdmissionClass, Hold } from "../../types";
import { ClassCreateForm } from "../../components/ClassCreateForm";
import { ErrorMessage } from "../../components/ErrorMessage";
import { LoadingState } from "../../components/LoadingState";
import { useAdmin } from "../../context/AdminContext";
import { usePolling } from "../../hooks/usePolling";
import { api } from "../../services/api";

const PAGE_SIZE = 20;
type Section = "overview" | "classes" | "applications" | "create";
type StatusFilter = "ALL" | Hold["status"];

function Pager({ page, pages, onChange, total, pageSize }: { page: number; pages: number; onChange: (p: number) => void; total: number; pageSize: number }) {
  if (pages <= 1) return <p className="pagination-caption">Showing {total ? 1 : 0}–{total} of {total}</p>;
  const first = (page - 1) * pageSize + 1;
  const last = Math.min(page * pageSize, total);
  return <div className="pagination"><span>{first}–{last} of {total}</span><div><button className="btn btn-ghost" disabled={page === 1} onClick={() => onChange(page - 1)}>Previous</button><span className="page-count">Page {page} of {pages}</span><button className="btn btn-ghost" disabled={page === pages} onClick={() => onChange(page + 1)}>Next</button></div></div>;
}

export function AdminDashboard() {
  const { session, logout } = useAdmin();
  const [section, setSection] = useState<Section>("overview");
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("ALL");
  const [applicationSearch, setApplicationSearch] = useState("");
  const [classSearch, setClassSearch] = useState("");
  const [applicationPage, setApplicationPage] = useState(1);
  const [classPage, setClassPage] = useState(1);
  const [selectedClass, setSelectedClass] = useState<AdmissionClass | null>(null);

  const { data: classes, error, loading, reload } = usePolling(() => api.listClasses(), 5000, "admin-classes");
  const { data: applications, error: applicationsError, loading: applicationsLoading } = usePolling(
    () => api.adminApplications(session!.token), 5000, `admin-applications:${session?.token ?? ""}`, !!session,
  );
  const allApplications = applications ?? [];
  const term = applicationSearch.trim().toLowerCase();
  const filteredApplications = useMemo(() => allApplications.filter(a =>
    (statusFilter === "ALL" || a.status === statusFilter) &&
    (!term || a.studentId.toLowerCase().includes(term) || a.className.toLowerCase().includes(term) || a.holdId.toLowerCase().includes(term))
  ), [allApplications, statusFilter, term]);
  const counts = useMemo(() => ({ ALL: allApplications.length, HELD: allApplications.filter(a => a.status === "HELD").length, CONFIRMED: allApplications.filter(a => a.status === "CONFIRMED").length, EXPIRED: allApplications.filter(a => a.status === "EXPIRED").length, CANCELLED: allApplications.filter(a => a.status === "CANCELLED").length }), [allApplications]);
  const filteredClasses = useMemo(() => (classes ?? []).filter(c => c.name.toLowerCase().includes(classSearch.trim().toLowerCase())), [classes, classSearch]);
  const applicationPages = Math.max(1, Math.ceil(filteredApplications.length / PAGE_SIZE));
  const classPages = Math.max(1, Math.ceil(filteredClasses.length / PAGE_SIZE));
  const pageApplications = filteredApplications.slice((applicationPage - 1) * PAGE_SIZE, applicationPage * PAGE_SIZE);
  const pageClasses = filteredClasses.slice((classPage - 1) * PAGE_SIZE, classPage * PAGE_SIZE);
  const stats = useMemo(() => { const list = classes ?? []; return { classes: list.length, seats: list.reduce((n,c) => n+c.totalSeats,0), held: list.reduce((n,c) => n+c.heldSeats,0), confirmed: list.reduce((n,c) => n+c.confirmedSeats,0), available: list.reduce((n,c) => n+c.availableSeats,0) }; }, [classes]);
  const selectedApplications = selectedClass ? allApplications.filter(a => a.classId === selectedClass.id) : [];

  if (!session) return null;
  const changeSection = (next: Section) => { setSection(next); setSelectedClass(null); };
  const nav = (key: Section, label: string, detail: string) => <button key={key} className={`admin-side-link${section === key ? " active" : ""}`} onClick={() => changeSection(key)}><span>{label}</span><small>{detail}</small></button>;

  return <div className="admin-shell">
    <aside className="admin-sidebar">
      <div className="sidebar-heading"><span className="sidebar-mark">SA</span><div><strong>School Admin</strong><small>Admission management</small></div></div>
      <p className="sidebar-label">WORKSPACE</p>
      {nav("overview", "Overview", "At a glance")}
      {nav("classes", "Class list", "Classes & enrolled students")}
      {nav("applications", "Application log", "All application statuses")}
      <p className="sidebar-label sidebar-label-spaced">MANAGEMENT</p>
      {nav("create", "Create class", "Add a class or shift")}
      <div className="sidebar-bottom"><span>Signed in as <strong>{session.username}</strong></span><button className="btn btn-ghost sidebar-logout" onClick={logout}>Log out</button></div>
    </aside>

    <main className="admin-main">
      <header className="admin-page-header"><div><p className="eyebrow">SCHOOL ADMISSION SYSTEM</p><h1>{selectedClass ? selectedClass.name : ({ overview: "Overview", classes: "Class list", applications: "Application log", create: "Create a class" } as Record<Section,string>)[section]}</h1><p className="muted">{selectedClass ? "Review the applications associated with this class and shift." : ({ overview: "A quick summary of admission activity.", classes: "Browse classes, check capacity and view enrolled students.", applications: "Search and review every application in one place.", create: "Set up a class and its independent seat capacity." } as Record<Section,string>)[section]}</p></div><span className="live-indicator"><span /> Live data · 5 sec</span></header>
      {(error || applicationsError) && <ErrorMessage message={error || applicationsError || "Unable to load data"} />}
      {section === "overview" && <>
        <div className="stats admin-stats"><div className="stat"><span>{stats.classes}</span>Classes</div><div className="stat"><span>{stats.seats}</span>Total seats</div><div className="stat"><span>{stats.held}</span>Held seats</div><div className="stat"><span>{stats.confirmed}</span>Enrolled</div><div className="stat"><span>{stats.available}</span>Available</div></div>
        <div className="overview-grid"><section className="admin-panel"><div className="panel-heading"><div><h2>Recent applications</h2><p className="muted">Latest activity across all classes</p></div><button className="text-action" onClick={() => changeSection("applications")}>View application log →</button></div>{applicationsLoading && !applications ? <LoadingState text="Loading applications…" /> : <ApplicationTable rows={[...allApplications].sort((a,b) => Date.parse(b.createdAt)-Date.parse(a.createdAt)).slice(0,8)} />}</section><section className="admin-panel"><div className="panel-heading"><div><h2>Classes</h2><p className="muted">Seat availability at a glance</p></div><button className="text-action" onClick={() => changeSection("classes")}>View all →</button></div>{loading && !classes ? <LoadingState text="Loading classes…" /> : <div className="compact-class-list">{(classes ?? []).slice(0,6).map(c => <button key={c.id} onClick={() => {setSelectedClass(c);setSection("classes");}}><span><strong>{c.name}</strong><small>{c.confirmedSeats} enrolled · {c.heldSeats} held</small></span><b>{c.availableSeats} left</b></button>)}</div>}</section></div>
      </>}

      {section === "classes" && <>
        {selectedClass ? <><button className="btn btn-ghost back-button" onClick={() => setSelectedClass(null)}>← Back to class list</button><div className="stats class-detail-stats"><div className="stat"><span>{selectedClass.totalSeats}</span>Total seats</div><div className="stat"><span>{selectedClass.confirmedSeats}</span>Enrolled</div><div className="stat"><span>{selectedClass.heldSeats}</span>Held</div><div className="stat"><span>{selectedClass.availableSeats}</span>Available</div></div><section className="admin-panel"><div className="panel-heading"><div><h2>Students and applications</h2><p className="muted">Confirmed applications represent enrolled students. HELD entries are temporary reservations.</p></div></div>{applicationsLoading && !applications ? <LoadingState text="Loading students…" /> : <ApplicationTable rows={selectedApplications.filter(a => a.status === "CONFIRMED" || a.status === "HELD")} empty="No students have applied to this class yet." />}</section></> : <section className="admin-panel"><div className="panel-heading"><div><h2>All classes</h2><p className="muted">Select a class to see its students and application statuses.</p></div></div><div className="toolbar"><input aria-label="Search classes" value={classSearch} onChange={e => {setClassSearch(e.target.value);setClassPage(1);}} placeholder="Search by class or shift name…" /></div>{loading && !classes ? <LoadingState text="Loading classes…" /> : <div className="table-wrap"><table><thead><tr><th>Class / shift</th><th>Total seats</th><th>Enrolled</th><th>Held</th><th>Available</th><th></th></tr></thead><tbody>{pageClasses.map(c => <tr key={c.id}><td><button className="table-link" onClick={() => setSelectedClass(c)}>{c.name}</button></td><td>{c.totalSeats}</td><td>{c.confirmedSeats}</td><td>{c.heldSeats}</td><td>{c.availableSeats}</td><td><button className="btn btn-ghost btn-small" onClick={() => setSelectedClass(c)}>View students</button></td></tr>)}</tbody></table>{filteredClasses.length === 0 && <p className="empty">No classes match your search.</p>}</div>}<Pager page={classPage} pages={classPages} total={filteredClasses.length} pageSize={PAGE_SIZE} onChange={setClassPage} /></section>}
      </>}

      {section === "applications" && <section className="admin-panel"><div className="panel-heading"><div><h2>All applications</h2><p className="muted">Includes held, confirmed, expired and cancelled records.</p></div><span className="record-count">{filteredApplications.length} records</span></div><div className="toolbar"><input aria-label="Search applications" value={applicationSearch} onChange={e => {setApplicationSearch(e.target.value);setApplicationPage(1);}} placeholder="Search student ID, class or application ID…" /></div><div className="status-filter-row" role="group" aria-label="Filter application status">{(["ALL","HELD","CONFIRMED","EXPIRED","CANCELLED"] as const).map(s => <button key={s} className={`filter-chip${statusFilter===s?" filter-chip-active":""}`} onClick={() => {setStatusFilter(s);setApplicationPage(1);}}>{s === "ALL" ? "All" : s}<span>{counts[s]}</span></button>)}</div>{applicationsLoading && !applications ? <LoadingState text="Loading applications…" /> : <ApplicationTable rows={pageApplications} empty="No applications match your search or filter." />}<Pager page={applicationPage} pages={applicationPages} total={filteredApplications.length} pageSize={PAGE_SIZE} onChange={setApplicationPage} /></section>}

      {section === "create" && <div className="create-class-wrap"><ClassCreateForm token={session.token} onCreated={() => {void reload();changeSection("classes");}} onUnauthorized={logout} /></div>}
    </main>
  </div>;
}

function ApplicationTable({ rows, empty = "No applications to show." }: { rows: Hold[]; empty?: string }) {
  if (!rows.length) return <p className="empty">{empty}</p>;
  return <div className="table-wrap"><table><thead><tr><th>Student birth registration no.</th><th>Class / shift</th><th>Application ID</th><th>Status</th><th>Applied at</th><th>Hold expires</th><th>Confirmed at</th></tr></thead><tbody>{rows.map(a => <tr key={a.holdId}><td className="mono">{a.studentId}</td><td>{a.className}</td><td className="mono small">{a.holdId}</td><td><StatusBadge status={a.status} /></td><td>{new Date(a.createdAt).toLocaleString()}</td><td>{a.status === "HELD" ? new Date(a.expiresAt).toLocaleString() : "—"}</td><td>{a.confirmedAt ? new Date(a.confirmedAt).toLocaleString() : "—"}</td></tr>)}</tbody></table></div>;
}
