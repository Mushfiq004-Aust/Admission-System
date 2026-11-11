import { Link, Navigate, Route, Routes } from "react-router-dom";
import { RequireAdmin } from "./components/RequireAdmin";
import { AdminLayout } from "./layouts/AdminLayout";
import { StudentLayout } from "./layouts/StudentLayout";
import { AdminDashboard } from "./pages/admin/AdminDashboard";
import { AdminLogin } from "./pages/admin/AdminLogin";
import { ApplicationDetails } from "./pages/ApplicationDetails";
import { ClassList } from "./pages/ClassList";
import { MyApplications } from "./pages/MyApplications";

export default function App() {
  return (
    <Routes>
      {/* Student / guardian portal (the default homepage). No login: the birth registration number is the identity. */}
      <Route element={<StudentLayout />}>
        <Route path="/" element={<ClassList />} />
        <Route path="/my-applications" element={<MyApplications />} />
        <Route path="/applications/:holdId" element={<ApplicationDetails />} />
        <Route
          path="*"
          element={
            <section>
              <h1>Page not found</h1>
              <p><Link to="/">Go to the class list</Link></p>
            </section>
          }
        />
      </Route>

      {/* Admin portal: /admin/login is public, everything else needs a valid admin session. */}
      <Route path="/admin" element={<AdminLayout />}>
        <Route path="login" element={<AdminLogin />} />
        <Route element={<RequireAdmin />}>
          <Route index element={<AdminDashboard />} />
        </Route>
        <Route path="*" element={<Navigate to="/admin" replace />} />
      </Route>
    </Routes>
  );
}
