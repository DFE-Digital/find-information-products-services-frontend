const { testAccessibility, testKeyboardNavigation, testColorContrast } = require('./setup');

describe('FIPS Accessibility Tests', () => {
  const pages = [
    { name: 'Home', url: '/' },
    { name: 'Products', url: '/Products' },
    { name: 'Categories', url: '/Categories' },
    { name: 'About', url: '/About' },
    { name: 'Contact', url: '/Contact' },
    { name: 'Data', url: '/Data' }
  ];

  pages.forEach(({ name, url }) => {
    describe(`${name} Page`, () => {
      beforeAll(async () => {
        await page.goto(url, { waitUntil: 'networkidle2' });
        await page.waitForTimeout(1000);
      });

      test('should have no accessibility violations', async () => {
        const results = await testAccessibility(page);
        
        expect(results.violations).toEqual([]);
        
        if (results.violations.length > 0) {
          console.log('Accessibility violations found:');
          results.violations.forEach(violation => {
            console.log(`- ${violation.id}: ${violation.description}`);
          });
        }
      });

      test('should have proper heading structure', async () => {
        const headings = await page.evaluate(() => {
          const headingElements = document.querySelectorAll('h1, h2, h3, h4, h5, h6');
          return Array.from(headingElements).map(h => ({
            tag: h.tagName,
            text: h.textContent.trim(),
            level: parseInt(h.tagName.charAt(1))
          }));
        });

        // Check that there's at least one h1
        const h1Elements = headings.filter(h => h.level === 1);
        expect(h1Elements.length).toBeGreaterThan(0);

        // Check heading hierarchy (no skipping levels)
        for (let i = 1; i < headings.length; i++) {
          const currentLevel = headings[i].level;
          const previousLevel = headings[i - 1].level;
          expect(currentLevel - previousLevel).toBeLessThanOrEqual(1);
        }
      });

      test('should have proper form labels', async () => {
        const formIssues = await page.evaluate(() => {
          const issues = [];
          const inputs = document.querySelectorAll('input, select, textarea');
          
          inputs.forEach(input => {
            const id = input.id;
            const type = input.type;
            
            // Skip hidden inputs
            if (type === 'hidden') return;
            
            const label = document.querySelector(`label[for="${id}"]`);
            const ariaLabel = input.getAttribute('aria-label');
            const ariaLabelledBy = input.getAttribute('aria-labelledby');
            
            if (!label && !ariaLabel && !ariaLabelledBy) {
              issues.push({
                element: input.outerHTML,
                id: id,
                type: type
              });
            }
          });
          
          return issues;
        });

        expect(formIssues).toEqual([]);
      });

      test('should have proper alt text for images', async () => {
        const imageIssues = await page.evaluate(() => {
          const issues = [];
          const images = document.querySelectorAll('img');
          
          images.forEach(img => {
            const alt = img.getAttribute('alt');
            const role = img.getAttribute('role');
            
            // Decorative images should have empty alt or role="presentation"
            if (img.getAttribute('role') === 'presentation') return;
            
            if (alt === null || alt === undefined) {
              issues.push({
                src: img.src,
                element: img.outerHTML
              });
            }
          });
          
          return issues;
        });

        expect(imageIssues).toEqual([]);
      });

      test('should support keyboard navigation', async () => {
        const focusableElements = await testKeyboardNavigation(page);
        
        expect(focusableElements.length).toBeGreaterThan(0);
        
        // Test tab navigation
        await page.keyboard.press('Tab');
        const firstFocused = await page.evaluate(() => document.activeElement.tagName);
        expect(firstFocused).toBeTruthy();
      });

      test('should have proper link text', async () => {
        const linkIssues = await page.evaluate(() => {
          const issues = [];
          const links = document.querySelectorAll('a[href]');
          
          links.forEach(link => {
            const text = link.textContent.trim();
            const ariaLabel = link.getAttribute('aria-label');
            const title = link.getAttribute('title');
            
            // Links should have descriptive text, aria-label, or title
            if (!text && !ariaLabel && !title) {
              issues.push({
                href: link.href,
                element: link.outerHTML
              });
            }
            
            // Avoid generic link text
            const genericTexts = ['click here', 'read more', 'more', 'link', 'here'];
            if (genericTexts.includes(text.toLowerCase())) {
              issues.push({
                href: link.href,
                text: text,
                element: link.outerHTML
              });
            }
          });
          
          return issues;
        });

        expect(linkIssues).toEqual([]);
      });

      test('should have proper focus indicators', async () => {
        const focusIssues = await page.evaluate(() => {
          const issues = [];
          const focusableElements = document.querySelectorAll(
            'a[href], button, input, select, textarea, [tabindex]:not([tabindex="-1"])'
          );
          
          focusableElements.forEach(element => {
            const styles = window.getComputedStyle(element);
            const outline = styles.outline;
            const outlineWidth = styles.outlineWidth;
            const outlineStyle = styles.outlineStyle;
            
            // Check if element has visible focus indicator
            if (outline === 'none' || outlineWidth === '0px' || outlineStyle === 'none') {
              // Check for custom focus styles
              const focusStyles = window.getComputedStyle(element, ':focus');
              const focusOutline = focusStyles.outline;
              const focusOutlineWidth = focusStyles.outlineWidth;
              
              if (focusOutline === 'none' || focusOutlineWidth === '0px') {
                issues.push({
                  tagName: element.tagName,
                  element: element.outerHTML
                });
              }
            }
          });
          
          return issues;
        });

        expect(focusIssues).toEqual([]);
      });

      test('should have proper ARIA labels where needed', async () => {
        const ariaIssues = await page.evaluate(() => {
          const issues = [];
          
          // Check for buttons without accessible names
          const buttons = document.querySelectorAll('button');
          buttons.forEach(button => {
            const text = button.textContent.trim();
            const ariaLabel = button.getAttribute('aria-label');
            const ariaLabelledBy = button.getAttribute('aria-labelledby');
            
            if (!text && !ariaLabel && !ariaLabelledBy) {
              issues.push({
                type: 'button',
                element: button.outerHTML
              });
            }
          });
          
          // Check for form controls without proper labels
          const formControls = document.querySelectorAll('input, select, textarea');
          formControls.forEach(control => {
            const id = control.id;
            const type = control.type;
            
            if (type === 'hidden') return;
            
            const label = document.querySelector(`label[for="${id}"]`);
            const ariaLabel = control.getAttribute('aria-label');
            const ariaLabelledBy = control.getAttribute('aria-labelledby');
            
            if (!label && !ariaLabel && !ariaLabelledBy) {
              issues.push({
                type: 'form-control',
                element: control.outerHTML
              });
            }
          });
          
          return issues;
        });

        expect(ariaIssues).toEqual([]);
      });
    });
  });

  describe('Global Accessibility Tests', () => {
    test('should have proper skip links', async () => {
      await page.goto('/', { waitUntil: 'networkidle2' });
      
      const skipLinks = await page.evaluate(() => {
        const links = document.querySelectorAll('a[href^="#"]');
        return Array.from(links).map(link => ({
          text: link.textContent.trim(),
          href: link.getAttribute('href')
        }));
      });

      // Should have at least one skip link
      expect(skipLinks.length).toBeGreaterThan(0);
    });

    test('should have proper page title', async () => {
      await page.goto('/', { waitUntil: 'networkidle2' });
      
      const title = await page.title();
      expect(title).toBeTruthy();
      expect(title.length).toBeGreaterThan(0);
    });

    test('should have proper language declaration', async () => {
      await page.goto('/', { waitUntil: 'networkidle2' });
      
      const htmlLang = await page.evaluate(() => {
        return document.documentElement.getAttribute('lang');
      });
      
      expect(htmlLang).toBeTruthy();
    });
  });
});
