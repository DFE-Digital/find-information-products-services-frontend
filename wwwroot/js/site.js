//
// For guidance on how to add JavaScript see:
// https://prototype-kit.service.gov.uk/docs/adding-css-javascript-and-images
//

// Application Insights helper functions
function trackEvent(eventName, properties) {
    if (window.appInsights) {
        window.appInsights.trackEvent(eventName, properties);
    }
}

function trackPageView(pageName, url) {
    if (window.appInsights) {
        window.appInsights.trackPageView(pageName, url);
    }
}

// Track page load
document.addEventListener('DOMContentLoaded', function() {
    // Track page view
    trackPageView(document.title, window.location.href);
    
    // Track user interactions
    trackEvent('PageLoaded', {
        page: window.location.pathname,
        referrer: document.referrer,
        userAgent: navigator.userAgent,
        timestamp: new Date().toISOString()
    });
});

// Feedback form functionality
document.addEventListener('DOMContentLoaded', function() {
  const feedbackLink = document.getElementById('feedback-link');
  const feedbackPanel = document.getElementById('feedback-panel');
  const thanksMessage = document.getElementById('thanksMessage');
  const feedbackForm = document.getElementById('feedback-form');
  const cancelButton = document.getElementById('cancelButton');

  if (feedbackLink && feedbackPanel && thanksMessage && feedbackForm && cancelButton) {
    // Show feedback panel when link is clicked
    feedbackLink.addEventListener('click', function(e) {
      e.preventDefault();
      feedbackPanel.classList.add('show');
      feedbackPanel.setAttribute('aria-hidden', 'false');
      thanksMessage.classList.remove('show');
      
      // Clear any validation errors when opening the panel
      const formGroup = document.getElementById('feedback_form_group');
      const errorSummary = document.getElementById('feedback-error-summary');
      const errorMessage = document.getElementById('feedback_form_input-error');
      const textarea = document.getElementById('feedback_form_input');
      
      if (formGroup && errorSummary && errorMessage && textarea) {
        formGroup.classList.remove('govuk-form-group--error');
        textarea.classList.remove('govuk-textarea--error');
        errorSummary.style.display = 'none';
        errorMessage.style.display = 'none';
        
        // Reset aria-describedby
        textarea.setAttribute('aria-describedby', 'feedback_form_input-info');
      }
      
      // Track feedback panel opened
      trackEvent('FeedbackPanelOpened', {
        page: window.location.pathname,
        timestamp: new Date().toISOString()
      });
      
      // Focus on the textarea
      if (textarea) {
        textarea.focus();
      }
    });

    // Hide feedback panel when cancel button is clicked
    cancelButton.addEventListener('click', function(e) {
      e.preventDefault();
      feedbackPanel.classList.remove('show');
      feedbackPanel.setAttribute('aria-hidden', 'true');
      
      // Clear any validation errors
      const formGroup = document.getElementById('feedback_form_group');
      const errorSummary = document.getElementById('feedback-error-summary');
      const errorMessage = document.getElementById('feedback_form_input-error');
      const textarea = document.getElementById('feedback_form_input');
      
      if (formGroup && errorSummary && errorMessage && textarea) {
        formGroup.classList.remove('govuk-form-group--error');
        textarea.classList.remove('govuk-textarea--error');
        errorSummary.style.display = 'none';
        errorMessage.style.display = 'none';
        
        // Reset aria-describedby
        textarea.setAttribute('aria-describedby', 'feedback_form_input-info');
      }
      
      // Track feedback panel cancelled
      trackEvent('FeedbackPanelCancelled', {
        page: window.location.pathname,
        timestamp: new Date().toISOString()
      });
    });

    // Function to show validation errors
    function showFeedbackError() {
      const formGroup = document.getElementById('feedback_form_group');
      const errorSummary = document.getElementById('feedback-error-summary');
      const errorMessage = document.getElementById('feedback_form_input-error');
      const textarea = document.getElementById('feedback_form_input');
      
      if (formGroup && errorSummary && errorMessage && textarea) {
        formGroup.classList.add('govuk-form-group--error');
        textarea.classList.add('govuk-textarea--error');
        errorSummary.style.display = 'block';
        errorMessage.style.display = 'block';
        
        // Update aria-describedby to include error message
        const currentDescribedBy = textarea.getAttribute('aria-describedby') || '';
        if (!currentDescribedBy.includes('feedback_form_input-error')) {
          textarea.setAttribute('aria-describedby', 'feedback_form_input-error ' + currentDescribedBy);
        }
        
        // Focus on error summary for screen readers
        errorSummary.focus();
      }
    }
    
    // Function to hide validation errors
    function hideFeedbackError() {
      const formGroup = document.getElementById('feedback_form_group');
      const errorSummary = document.getElementById('feedback-error-summary');
      const errorMessage = document.getElementById('feedback_form_input-error');
      const textarea = document.getElementById('feedback_form_input');
      
      if (formGroup && errorSummary && errorMessage && textarea) {
        formGroup.classList.remove('govuk-form-group--error');
        textarea.classList.remove('govuk-textarea--error');
        errorSummary.style.display = 'none';
        errorMessage.style.display = 'none';
        
        // Update aria-describedby to remove error message
        const currentDescribedBy = textarea.getAttribute('aria-describedby') || '';
        textarea.setAttribute('aria-describedby', currentDescribedBy.replace('feedback_form_input-error', '').trim());
      }
    }
    
    // Clear errors when user starts typing and is under limit
    const textarea = feedbackForm.querySelector('textarea');
    if (textarea) {
      textarea.addEventListener('input', function() {
        if (this.value.length <= 1000) {
          hideFeedbackError();
        }
      });
    }
    
    // Handle form submission
    feedbackForm.addEventListener('submit', function(e) {
      e.preventDefault();
      
      const textarea = feedbackForm.querySelector('textarea');
      const feedbackText = textarea.value.trim();
      
      // Validate character count
      if (feedbackText.length > 1000) {
        showFeedbackError();
        
        // Track validation error
        trackEvent('FeedbackValidationError', {
          page: window.location.pathname,
          characterCount: feedbackText.length,
          timestamp: new Date().toISOString()
        });
        
        return;
      }
      
      // Clear any existing errors
      hideFeedbackError();
      
      if (feedbackText) {
        // Track feedback submission
        trackEvent('FeedbackSubmitted', {
          page: window.location.pathname,
          feedbackLength: feedbackText.length,
          timestamp: new Date().toISOString()
        });
        
        // Send feedback to the server
        const submitButton = feedbackForm.querySelector('button[type="submit"]');
        const originalButtonText = submitButton.textContent;
        submitButton.textContent = 'Submitting...';
        submitButton.disabled = true;
        
        fetch('/Contact/SubmitFeedback', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json'
          },
          body: JSON.stringify({
            feedbackFormInput: feedbackText
          })
        })
        .then(response => response.json())
        .then(data => {
          if (data.success) {
            // Show success message
            feedbackPanel.classList.remove('show');
            feedbackPanel.setAttribute('aria-hidden', 'true');
            thanksMessage.classList.add('show');
            
            // Clear the form and errors
            textarea.value = '';
            hideFeedbackError();
            
            // Focus on the thank you message for screen readers
            thanksMessage.focus();
            
            console.log('Feedback submitted successfully');
          } else {
            // Show error message
            alert('Sorry, there was an error submitting your feedback. Please try again.');
            console.error('Feedback submission failed:', data.message);
          }
        })
        .catch(error => {
          // Show error message
          alert('Sorry, there was an error submitting your feedback. Please try again.');
          console.error('Feedback submission error:', error);
        })
        .finally(() => {
          // Reset button state
          submitButton.textContent = originalButtonText;
          submitButton.disabled = false;
        });
      }
    });
  }
});

// Track search interactions
document.addEventListener('DOMContentLoaded', function() {
    const searchForms = document.querySelectorAll('form[action*="search"], form[action*="Search"]');
    searchForms.forEach(function(form) {
        form.addEventListener('submit', function(e) {
            const searchInput = form.querySelector('input[type="search"], input[name*="search"], input[name*="Search"]');
            if (searchInput && searchInput.value.trim()) {
                trackEvent('SearchPerformed', {
                    searchTerm: searchInput.value.trim(),
                    page: window.location.pathname,
                    timestamp: new Date().toISOString()
                });
            }
        });
    });
});

// Track product clicks
document.addEventListener('DOMContentLoaded', function() {
    const productLinks = document.querySelectorAll('a[href*="/products/"], a[href*="/product/"]');
    productLinks.forEach(function(link) {
        link.addEventListener('click', function(e) {
            trackEvent('ProductClicked', {
                productUrl: link.href,
                productText: link.textContent.trim(),
                page: window.location.pathname,
                timestamp: new Date().toISOString()
            });
        });
    });
});

// Track category clicks
document.addEventListener('DOMContentLoaded', function() {
    const categoryLinks = document.querySelectorAll('a[href*="/categories/"], a[href*="/category/"]');
    categoryLinks.forEach(function(link) {
        link.addEventListener('click', function(e) {
            trackEvent('CategoryClicked', {
                categoryUrl: link.href,
                categoryText: link.textContent.trim(),
                page: window.location.pathname,
                timestamp: new Date().toISOString()
            });
        });
    });
});

// Track errors
window.addEventListener('error', function(e) {
    trackEvent('JavaScriptError', {
        errorMessage: e.message,
        errorSource: e.filename,
        errorLine: e.lineno,
        errorColumn: e.colno,
        page: window.location.pathname,
        timestamp: new Date().toISOString()
    });
});

// Track unhandled promise rejections
window.addEventListener('unhandledrejection', function(e) {
    trackEvent('UnhandledPromiseRejection', {
        errorMessage: e.reason ? e.reason.toString() : 'Unknown error',
        page: window.location.pathname,
        timestamp: new Date().toISOString()
    });
});

