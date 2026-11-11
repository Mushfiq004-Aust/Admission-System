import { Outlet } from "react-router-dom";
// The admin workspace owns its own sidebar and responsive layout.
export function AdminLayout() {
  return <Outlet />;
}
