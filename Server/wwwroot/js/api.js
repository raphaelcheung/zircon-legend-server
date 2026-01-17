/**
 * 皓石传奇三 - API 请求封装
 * Zircon Mir3 Server Admin API Client
 */

const API_BASE_URL = window.location.origin;

/**
 * API 请求封装
 */
const api = {
  /**
   * 获取请求头
   * @returns {Headers}
   */
  getHeaders() {
    const headers = new Headers({
      'Content-Type': 'application/json',
    });

    const token = this.getToken();
    if (token) {
      headers.append('Authorization', `Bearer ${token}`);
    }

    return headers;
  },

  /**
   * 获取存储的 Token
   * @returns {string|null}
   */
  getToken() {
    return localStorage.getItem('token') || sessionStorage.getItem('token');
  },

  /**
   * 处理响应
   * @param {Response} response
   * @returns {Promise<any>}
   */
  async handleResponse(response) {
    const contentType = response.headers.get('content-type');
    const isJson = contentType && contentType.includes('application/json');

    if (!response.ok) {
      if (response.status === 401) {
        // Token 过期，尝试刷新
        const refreshed = await this.tryRefreshToken();
        if (!refreshed) {
          // 刷新失败，跳转登录
          this.clearAuth();
          window.location.href = 'index.html';
          throw new Error('登录已过期，请重新登录');
        }
        throw new Error('RETRY'); // 标记需要重试
      }

      const error = isJson ? await response.json() : await response.text();
      throw new Error(error.message || error || `请求失败: ${response.status}`);
    }

    if (isJson) {
      return response.json();
    }

    return response.text();
  },

  /**
   * 尝试刷新 Token
   * @returns {Promise<boolean>}
   */
  async tryRefreshToken() {
    const refreshToken = localStorage.getItem('refreshToken') || sessionStorage.getItem('refreshToken');
    if (!refreshToken) {
      return false;
    }

    try {
      const response = await fetch(`${API_BASE_URL}/api/auth/refresh`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ refreshToken }),
      });

      if (!response.ok) {
        return false;
      }

      const data = await response.json();
      const storage = localStorage.getItem('token') ? localStorage : sessionStorage;
      storage.setItem('token', data.token);
      storage.setItem('refreshToken', data.refreshToken);

      return true;
    } catch {
      return false;
    }
  },

  /**
   * 清除认证信息
   */
  clearAuth() {
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
    sessionStorage.removeItem('token');
    sessionStorage.removeItem('refreshToken');
    sessionStorage.removeItem('user');
  },

  /**
   * GET 请求
   * @param {string} endpoint - API 端点
   * @param {object} params - 查询参数
   * @returns {Promise<any>}
   */
  async get(endpoint, params = {}) {
    const url = new URL(`${API_BASE_URL}${endpoint}`);
    Object.keys(params).forEach(key => {
      if (params[key] !== undefined && params[key] !== null) {
        url.searchParams.append(key, params[key]);
      }
    });

    const response = await fetch(url.toString(), {
      method: 'GET',
      headers: this.getHeaders(),
    });

    return this.handleResponse(response);
  },

  /**
   * POST 请求
   * @param {string} endpoint - API 端点
   * @param {object} data - 请求体数据
   * @returns {Promise<any>}
   */
  async post(endpoint, data = {}) {
    const response = await fetch(`${API_BASE_URL}${endpoint}`, {
      method: 'POST',
      headers: this.getHeaders(),
      body: JSON.stringify(data),
    });

    return this.handleResponse(response);
  },

  /**
   * PUT 请求
   * @param {string} endpoint - API 端点
   * @param {object} data - 请求体数据
   * @returns {Promise<any>}
   */
  async put(endpoint, data = {}) {
    const response = await fetch(`${API_BASE_URL}${endpoint}`, {
      method: 'PUT',
      headers: this.getHeaders(),
      body: JSON.stringify(data),
    });

    return this.handleResponse(response);
  },

  /**
   * DELETE 请求
   * @param {string} endpoint - API 端点
   * @returns {Promise<any>}
   */
  async delete(endpoint) {
    const response = await fetch(`${API_BASE_URL}${endpoint}`, {
      method: 'DELETE',
      headers: this.getHeaders(),
    });

    return this.handleResponse(response);
  },
};

// 导出（如果使用模块）
if (typeof module !== 'undefined' && module.exports) {
  module.exports = api;
}
