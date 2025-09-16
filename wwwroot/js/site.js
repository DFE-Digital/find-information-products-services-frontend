//
// For guidance on how to add JavaScript see:
// https://prototype-kit.service.gov.uk/docs/adding-css-javascript-and-images
//

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
      
      // Focus on the textarea
      const textarea = feedbackForm.querySelector('textarea');
      if (textarea) {
        textarea.focus();
      }
    });

    // Hide feedback panel when cancel button is clicked
    cancelButton.addEventListener('click', function(e) {
      e.preventDefault();
      feedbackPanel.classList.remove('show');
      feedbackPanel.setAttribute('aria-hidden', 'true');
    });

    // Handle form submission
    feedbackForm.addEventListener('submit', function(e) {
      e.preventDefault();
      
      const textarea = feedbackForm.querySelector('textarea');
      const feedbackText = textarea.value.trim();
      
      if (feedbackText) {
        // Here you would typically send the feedback to your server
        // For now, we'll just show the thank you message
        feedbackPanel.classList.remove('show');
        feedbackPanel.setAttribute('aria-hidden', 'true');
        thanksMessage.classList.add('show');
        
        // Clear the form
        textarea.value = '';
        
        // Focus on the thank you message for screen readers
        thanksMessage.focus();
      }
    });
  }
});

