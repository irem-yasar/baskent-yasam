import React from "react";
import { BrowserRouter as Router, Routes, Route } from "react-router-dom";

import LoginPage from "./pages/LoginPage";
import StudentDashboard from "./pages/StudentDashboard";
import TeacherAppointmentPage from "./pages/TeacherAppointmentPage";

function App() {
  return (
    <Router>
      <Routes>
        <Route path="/" element={<LoginPage />} />
        <Route path="/ogrenci" element={<StudentDashboard />} />
        <Route path="/randevu" element={<TeacherAppointmentPage />} />
      </Routes>
    </Router>
  );
}

export default App;
