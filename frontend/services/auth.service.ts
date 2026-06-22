import keycloak from '../config/keycloak';

interface KeycloakTokenParsed {
  sub?: string;
  email?: string;
  name?: string;
  preferred_username?: string;
  realm_access?: {
    roles: string[];
  };
}

export interface User {
  id: string;
  email: string;
  name: string;
  role: 'customer' | 'admin' | 'seller';
}

class AuthService {
  private initialized = false;
  private initPromise: Promise<boolean> | null = null;

  async init(): Promise<boolean> {
    if (this.initialized) return true;
    
    if (!this.initPromise) {
      this.initPromise = new Promise(async (resolve) => {
        try {
          const authenticated = await keycloak.init({
            onLoad: 'check-sso',
            silentCheckSsoRedirectUri: window.location.origin + '/silent-check-sso.html',
          });
          this.initialized = true;
          console.log('Keycloak инициализирован, статус:', authenticated);
          resolve(authenticated);
        } catch (error) {
          console.error('Ошибка инициализации Keycloak:', error);
          resolve(false);
        }
      });
    }
    
    return this.initPromise;
  }

  login(): void {
    keycloak.login();
  }

  logout(): void {
    keycloak.logout();
  }

  register(): void {
    keycloak.register();
  }

  getToken(): string | null {
    return keycloak.token || null;
  }

  isAuthenticated(): boolean {
    return !!keycloak.authenticated && !!keycloak.token;
  }

  getUser(): User | null {
    if (!keycloak.authenticated || !keycloak.tokenParsed) {
      return null;
    }

    const parsed = keycloak.tokenParsed as KeycloakTokenParsed;
    const roles = parsed.realm_access?.roles || [];
    
    let role: 'customer' | 'admin' | 'seller' = 'customer';
    if (roles.includes('admin')) {
      role = 'admin';
    } else if (roles.includes('seller')) {
      role = 'seller';
    }

    return {
      id: parsed.sub || '',
      email: parsed.email || '',
      name: parsed.name || parsed.preferred_username || '',
      role: role,
    };
  }

  async updateToken(minValidity: number = 5): Promise<boolean> {
    try {
      if (!keycloak.authenticated) {
        return false;
      }

      if (!keycloak.refreshToken) {
        return false;
      }

      return await keycloak.updateToken(minValidity);
    } catch {
      return false;
    }
  }
}

export const authService = new AuthService();