import axios, { AxiosInstance, InternalAxiosRequestConfig, AxiosError } from 'axios';

// API base URL'i environment variable'dan al
const API_BASE_URL = process.env.REACT_APP_API_BASE_URL || 'http://localhost:5283/api';

// Axios instance oluştur
const apiClient: AxiosInstance = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 10000, // 10 saniye timeout
});

// Request interceptor - Her istekten önce çalışır
apiClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    // LocalStorage'dan token'ı al ve header'a ekle
    const token = localStorage.getItem('token');
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
      // Debug için token'ın gönderildiğini kontrol et (sadece ilk 20 karakter)
      if (config.url && !config.url.includes('/Auth/')) {
        console.log(`Token gönderiliyor - ${config.method?.toUpperCase()} ${config.url}`);
      }
    } else {
      console.warn('Token bulunamadı! İstek authorization olmadan gönderiliyor.');
    }
    return config;
  },
  (error: AxiosError) => {
    return Promise.reject(error);
  }
);

// Response interceptor - Her yanıttan önce çalışır
apiClient.interceptors.response.use(
  (response) => {
    return response;
  },
  (error: AxiosError) => {
    // 401 Unauthorized hatası durumunda token'ı temizle ve login sayfasına yönlendir
    if (error.response?.status === 401) {
      const url = error.config?.url || '';
      const method = error.config?.method?.toUpperCase() || 'GET';
      
      console.warn(`401 Unauthorized - ${method} ${url}`);
      console.warn('Error details:', error.response?.data);
      
      // Auth endpoint'leri için yönlendirme yapma (login/register sırasında 401 normal olabilir)
      const isAuthEndpoint = url.includes('/Auth/login') || 
                             url.includes('/Auth/register') || 
                             url.includes('/Auth/teachers');
      
      if (!isAuthEndpoint) {
        // Token'ı temizle
        const token = localStorage.getItem('token');
        if (token) {
          console.warn('Token mevcut ama geçersiz. Temizleniyor...');
          console.warn('Token içeriği (ilk 50 karakter):', token.substring(0, 50));
          
          // Token'ı decode etmeye çalış (JWT formatında mı kontrol et)
          try {
            const tokenParts = token.split('.');
            if (tokenParts.length === 3) {
              const payload = JSON.parse(atob(tokenParts[1]));
              console.warn('Token payload:', payload);
              const expDate = payload.exp ? new Date(payload.exp * 1000) : null;
              const now = new Date();
              console.warn('Token exp (UTC):', expDate);
              console.warn('Token şimdi (Local):', now);
              console.warn('Token exp (Local):', expDate ? new Date(expDate.getTime() + (now.getTimezoneOffset() * 60000)) : 'Yok');
              if (payload.exp && payload.exp * 1000 < Date.now()) {
                console.warn('Token süresi dolmuş!');
              } else if (payload.exp) {
                const remainingMinutes = Math.floor((payload.exp * 1000 - Date.now()) / 60000);
                console.warn(`Token süresi: ${remainingMinutes} dakika kaldı`);
              }
            }
          } catch (e) {
            console.warn('Token decode edilemedi:', e);
          }
          
          localStorage.removeItem('token');
          localStorage.removeItem('user');
        } else {
          console.warn('Token bulunamadı!');
        }
        
        // Sadece bir kez yönlendirme yap (sonsuz döngüyü önlemek için)
        const currentPath = window.location.pathname;
        if (currentPath !== '/' && !currentPath.includes('/login')) {
          console.warn(`Login sayfasına yönlendiriliyor. Mevcut sayfa: ${currentPath}`);
          // React Router ile uyumlu yönlendirme için window.location kullan
          // Ama önce kullanıcıya bilgi ver
          alert('Oturum süreniz dolmuş. Lütfen tekrar giriş yapın.');
          window.location.href = '/';
        }
      } else {
        console.log('Auth endpoint için 401 hatası - yönlendirme yapılmıyor');
      }
    }
    return Promise.reject(error);
  }
);

export default apiClient;

