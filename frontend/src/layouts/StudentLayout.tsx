import { Link, Outlet } from "react-router-dom";
import { Navbar } from "../components/Navbar";
import { StudentIdentityInput } from "../components/StudentIdentityInput";

// The parent / guardian portal: student identity box on top, simple navigation, no admin links.
export function StudentLayout() {
  return (
    <>
      <header className="topbar">
        <div className="topbar-inner">
          <StudentIdentityInput />
          <div className="brand-row">
            <Link to="/" className="brand">School Admission</Link>
            <Navbar />
          </div>
        </div>
      </header>
      <main className="page">
        <Outlet />
      </main>
      <footer className="footer">
        Seats are held for a limited time. Pay the admission fee before the countdown ends to confirm your child's seat.
      </footer>
    </>
  );
}
