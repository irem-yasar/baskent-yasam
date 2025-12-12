import React, { useState } from "react";
import { useNavigate } from "react-router-dom";

const TitleContainer: React.FC = () => (
  <h1 className="text-2xl font-semibold text-center text-slate-800">
    Başkent Yaşam Uygulaması
  </h1>
);

type InputContainerProps = {
  type: string;
  placeholder: string;
  value: string;
  onChange: (value: string) => void;
};

const InputContainer: React.FC<InputContainerProps> = ({
  type,
  placeholder,
  value,
  onChange,
}) => (
  <div>
    <input
      type={type}
      placeholder={placeholder}
      value={value}
      onChange={(e) => onChange(e.target.value)}
      className="w-full border border-slate-300 rounded-lg px-4 py-3 text-slate-700 placeholder-slate-500 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
    />
  </div>
);

const SubmitButtonContainer: React.FC = () => (
  <button
    type="submit"
    className="w-full bg-[#d71920] hover:bg-blue-700 text-white font-medium py-3 rounded-lg transition"
  >
    Giriş yap
  </button>
);

const ForgotPasswordContainer: React.FC = () => (
  <a
    href="#"
    className="text-blue-600 text-sm underline text-center hover:text-blue-700"
  >
    Şifrenizi mi unuttunuz?
  </a>
);

const LoginPage: React.FC = () => {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");

  const navigate = useNavigate();

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    navigate("/ogrenci");
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-slate-100">
      <form
        onSubmit={handleSubmit}
        className="w-full max-w-sm flex flex-col gap-4"
      >
        <TitleContainer />

        <InputContainer
          type="text"
          placeholder="Kullanıcı adı"
          value={username}
          onChange={setUsername}
        />

        <InputContainer
          type="password"
          placeholder="Şifre"
          value={password}
          onChange={setPassword}
        />

        <SubmitButtonContainer />

        <ForgotPasswordContainer />
      </form>
    </div>
  );
};

export default LoginPage;
