import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import App from "./App";
import { AdminProvider } from "./context/AdminContext";
import { StudentProvider } from "./context/StudentContext";
import "./styles.css";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <BrowserRouter>
      <StudentProvider>
        <AdminProvider>
          <App />
        </AdminProvider>
      </StudentProvider>
    </BrowserRouter>
  </StrictMode>,
);
