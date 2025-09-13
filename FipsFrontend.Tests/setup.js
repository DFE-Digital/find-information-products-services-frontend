const axe = require('axe-core');

// Inject axe-core into the page
beforeAll(async () => {
  await page.addScriptTag({
    url: 'https://cdnjs.cloudflare.com/ajax/libs/axe-core/4.8.4/axe.min.js'
  });
});

// Global accessibility test helper
global.testAccessibility = async (page, options = {}) => {
  const results = await page.evaluate(() => {
    return new Promise((resolve) => {
      axe.run(document, {
        tags: ['wcag2a', 'wcag2aa', 'wcag21aa'],
        ...options
      }, (err, results) => {
        if (err) {
          resolve({ error: err.toString() });
        } else {
          resolve(results);
        }
      });
    });
  });

  return results;
};

// Global keyboard navigation test helper
global.testKeyboardNavigation = async (page) => {
  const tabOrder = [];
  
  // Start from the beginning of the page
  await page.keyboard.press('Tab');
  
  // Get all focusable elements
  const focusableElements = await page.evaluate(() => {
    const elements = document.querySelectorAll(
      'a[href], button, input, select, textarea, [tabindex]:not([tabindex="-1"])'
    );
    return Array.from(elements).map(el => ({
      tagName: el.tagName,
      text: el.textContent?.trim() || '',
      href: el.href || '',
      type: el.type || '',
      id: el.id || '',
      className: el.className || ''
    }));
  });

  return focusableElements;
};

// Global color contrast test helper
global.testColorContrast = async (page) => {
  const contrastResults = await page.evaluate(() => {
    const results = [];
    const elements = document.querySelectorAll('*');
    
    elements.forEach(element => {
      const styles = window.getComputedStyle(element);
      const color = styles.color;
      const backgroundColor = styles.backgroundColor;
      
      if (color && backgroundColor && color !== 'rgba(0, 0, 0, 0)' && backgroundColor !== 'rgba(0, 0, 0, 0)') {
        results.push({
          element: element.tagName,
          color: color,
          backgroundColor: backgroundColor,
          textContent: element.textContent?.trim().substring(0, 50) || ''
        });
      }
    });
    
    return results;
  });

  return contrastResults;
};
