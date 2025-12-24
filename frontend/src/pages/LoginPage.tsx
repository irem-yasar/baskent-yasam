import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import { login, ApiError } from "../services/authService";

const LoginPage: React.FC = () => {
  const navigate = useNavigate();

  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [role, setRole] = useState<"student" | "instructor">("student");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string>("");

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setLoading(true);

    try {
      const response = await login({ username, password, role });
      
      // Başarılı giriş sonrası role göre yönlendir
      if (response.user.role === "student") {
        navigate("/ogrenci");
      } else {
        navigate("/ogretim-elemani");
      }
    } catch (err) {
      const apiError = err as ApiError;
      setError(apiError.message || "Giriş yapılırken bir hata oluştu");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-slate-100">
      <form
        onSubmit={handleSubmit}
        className="w-full max-w-sm bg-white p-6 rounded-xl shadow flex flex-col gap-4"
      >
        <h1 className="text-xl font-semibold text-center text-slate-800">
          Başkent Yaşam Giriş
        </h1>

        {error && (
          <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded">
            {error}
          </div>
        )}

        <input
          type="text"
          placeholder="Kullanıcı adı"
          value={username}
          onChange={(e) => setUsername(e.target.value)}
          className="border rounded-lg px-4 py-2"
        />

        <input
          type="password"
          placeholder="Şifre"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          className="border rounded-lg px-4 py-2"
        />

        <select
          value={role}
          onChange={(e) => setRole(e.target.value as "student" | "instructor")}
          className="border rounded-lg px-4 py-2"
        >
          <option value="student">Öğrenci</option>
          <option value="instructor">Öğretim Elemanı</option>
        </select>

        <button
          type="submit"
          disabled={loading}
          className="bg-[#d71920] text-white py-2 rounded-lg hover:opacity-90 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {loading ? "Giriş yapılıyor..." : "Giriş yap"}
        </button>
      </form>
    </div>
  );
};

export default LoginPage;
