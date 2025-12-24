import apiClient from '../api/axios';

export interface LoginRequest {
  username: string;
  password: string;
  role?: 'student' | 'instructor';
}

// Backend'den gelen response formatı
export interface BackendLoginResponse {
  token: string;
  userId: number;
  name: string;
  role: string; // "Student" veya "Instructor" (büyük harf ile)
}

// Frontend'in kullandığı format
export interface LoginResponse {
  token: string;
  user: {
    id: string;
    username: string;
    role: 'student' | 'instructor';
    name?: string;
  };
}

export interface ApiError {
  message: string;
  status?: number;
}

// Login işlemi
export const login = async (credentials: LoginRequest): Promise<LoginResponse> => {
  try {
    // Backend'in beklediği formata göre request body'yi hazırla
    // Backend sadece usernameOrEmail ve password bekliyor
    // email alanını göndermiyoruz çünkü backend'de property çakışmasına neden oluyor
    const requestBody = {
      usernameOrEmail: credentials.username, // Kullanıcı adı veya email
      password: credentials.password,
    };

    console.log('Login request body:', requestBody); // Debug için

    // Backend endpoint'i /Auth/login (büyük harf ile)
    const response = await apiClient.post<BackendLoginResponse>('/Auth/login', requestBody);
    
    // Backend'den gelen response'u frontend formatına dönüştür
    const backendData = response.data;
    
    // Role'ü küçük harfe çevir ve normalize et
    const normalizedRole = backendData.role.toLowerCase() as 'student' | 'instructor';
    
    // Frontend formatına dönüştür
    const loginResponse: LoginResponse = {
      token: backendData.token,
      user: {
        id: backendData.userId.toString(),
        username: backendData.name, // Backend'de username yok, name kullanıyoruz
        role: normalizedRole,
        name: backendData.name,
      },
    };
    
    // Token ve kullanıcı bilgilerini localStorage'a kaydet
    if (loginResponse.token) {
      localStorage.setItem('token', loginResponse.token);
      localStorage.setItem('user', JSON.stringify(loginResponse.user));
    }
    
    return loginResponse;
  } catch (error: any) {
    console.error('Login error:', error.response?.data); // Debug için
    throw {
      message: error.response?.data?.message || error.response?.data?.error || 'Giriş yapılırken bir hata oluştu',
      status: error.response?.status,
    } as ApiError;
  }
};

// Logout işlemi
export const logout = (): void => {
  localStorage.removeItem('token');
  localStorage.removeItem('user');
};

// Kullanıcı bilgilerini al
export const getCurrentUser = () => {
  const userStr = localStorage.getItem('user');
  if (userStr) {
    return JSON.parse(userStr);
  }
  return null;
};

// Token kontrolü
export const isAuthenticated = (): boolean => {
  return !!localStorage.getItem('token');
};

