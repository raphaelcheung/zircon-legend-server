/**
 * 皓石传奇三 - 认证管理
 * Zircon Mir3 Server Admin Auth Manager
 */

/**
 * 检查是否已登录
 * @returns {boolean}
 */
function checkAuth() {
  const token = localStorage.getItem('token') || sessionStorage.getItem('token');
  return !!token;
}

/**
 * 获取当前用户信息
 * @returns {object|null}
 */
function getUser() {
  const userStr = localStorage.getItem('user') || sessionStorage.getItem('user');
  if (!userStr) {
    return null;
  }

  try {
    return JSON.parse(userStr);
  } catch {
    return null;
  }
}

/**
 * 获取用户权限等级
 * @returns {string|null}
 */
function getUserIdentity() {
  const user = getUser();
  return user ? user.identity : null;
}

/**
 * 检查是否有指定权限
 * @param {string} requiredIdentity - 需要的权限等级
 * @returns {boolean}
 */
function hasPermission(requiredIdentity) {
  const identityLevels = {
    'Normal': 0,
    'Observer': 1,
    'Operator': 2,
    'Admin': 3,
    'SuperAdmin': 4,
  };

  const userIdentity = getUserIdentity();
  if (!userIdentity) {
    return false;
  }

  const userLevel = identityLevels[userIdentity] || 0;
  const requiredLevel = identityLevels[requiredIdentity] || 0;

  return userLevel >= requiredLevel;
}

/**
 * 登出
 */
function logout() {
  localStorage.removeItem('token');
  localStorage.removeItem('refreshToken');
  localStorage.removeItem('user');
  sessionStorage.removeItem('token');
  sessionStorage.removeItem('refreshToken');
  sessionStorage.removeItem('user');
}

/**
 * 需要登录的页面守卫
 * 如果未登录则跳转到登录页
 */
function requireAuth() {
  if (!checkAuth()) {
    window.location.href = 'index.html';
    return false;
  }
  return true;
}

/**
 * 需要特定权限的页面守卫
 * @param {string} requiredIdentity - 需要的权限等级
 */
function requirePermission(requiredIdentity) {
  if (!requireAuth()) {
    return false;
  }

  if (!hasPermission(requiredIdentity)) {
    alert('权限不足');
    window.location.href = 'dashboard.html';
    return false;
  }

  return true;
}

// 导出（如果使用模块）
if (typeof module !== 'undefined' && module.exports) {
  module.exports = {
    checkAuth,
    getUser,
    getUserIdentity,
    hasPermission,
    logout,
    requireAuth,
    requirePermission,
  };
}
