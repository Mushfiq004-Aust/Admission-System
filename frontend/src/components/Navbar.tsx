import { NavLink } from "react-router-dom";

// Student navigation only. The admin portal is separate (/admin) and is not linked from here.
export function Navbar() {
  const cls = ({ isActive }: { isActive: boolean }) => `nav-link${isActive ? " nav-link-active" : ""}`;
  return (
    <nav className="nav" aria-label="Main">
      <NavLink to="/" end className={cls}>Classes</NavLink>
      <NavLink to="/my-applications" className={cls}>My Applications</NavLink>
    </nav>
  );
}
