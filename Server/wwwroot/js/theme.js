/**
 * 皓石传奇三 - 主题切换
 * Zircon Mir3 Server Admin Theme Manager
 */

// 初始化主题
(function initTheme() {
  const savedTheme = localStorage.getItem('theme') || 'dark';
  document.documentElement.setAttribute('data-theme', savedTheme);
})();

/**
 * 切换主题
 */
function toggleTheme() {
  const html = document.documentElement;
  const currentTheme = html.getAttribute('data-theme');
  const newTheme = currentTheme === 'dark' ? 'light' : 'dark';

  html.setAttribute('data-theme', newTheme);
  localStorage.setItem('theme', newTheme);

  // 可选：添加切换动画类
  html.classList.add('theme-transitioning');
  setTimeout(() => {
    html.classList.remove('theme-transitioning');
  }, 300);
}

/**
 * 设置特定主题
 * @param {string} theme - 'dark' 或 'light'
 */
function setTheme(theme) {
  if (theme !== 'dark' && theme !== 'light') {
    console.warn('Invalid theme:', theme);
    return;
  }

  document.documentElement.setAttribute('data-theme', theme);
  localStorage.setItem('theme', theme);
}

/**
 * 获取当前主题
 * @returns {string} 当前主题
 */
function getTheme() {
  return document.documentElement.getAttribute('data-theme') || 'dark';
}
