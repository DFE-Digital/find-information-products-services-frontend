//
// For guidance on how to add JavaScript see:
// https://prototype-kit.service.gov.uk/docs/adding-css-javascript-and-images
//

window.GOVUKPrototypeKit.documentReady(() => {
    // Feedback panel functionality
    const feedbackLink = document.getElementById('feedback-link');
    const feedbackPanel = document.getElementById('feedback-panel');
    const feedbackForm = document.getElementById('feedback-form');
    const cancelButton = document.getElementById('cancelButton');
    const feedbackLinkText = document.getElementById('feedback-link-text');
  
    const thanksMessage = document.getElementById('thanksMessage');
    const feedbackInput = document.getElementById('feedback_form_input');
  
    if (feedbackLink && feedbackPanel) {
      // Open feedback panel
      feedbackLink.addEventListener('click', function(e) {
        e.preventDefault();
        
        // Hide the link text
        feedbackLinkText.style.display = 'none';
        
        // Show the feedback panel
        feedbackPanel.style.display = 'block';
        feedbackPanel.setAttribute('aria-hidden', 'false');
        
        // Focus on the textarea for accessibility
        if (feedbackInput) {
          feedbackInput.focus();
        }
      });
  
      // Close feedback panel
      function closeFeedbackPanel() {
        feedbackPanel.style.display = 'none';
        feedbackPanel.setAttribute('aria-hidden', 'true');
        
        // Show the link text
        feedbackLinkText.style.display = 'block';
        
        // Clear the form
        if (feedbackForm) {
          feedbackForm.reset();
        }
      }
  
      // Cancel button
      if (cancelButton) {
        cancelButton.addEventListener('click', function(e) {
          e.preventDefault();
          closeFeedbackPanel();
        });
      }
  
      // Form submission
      if (feedbackForm) {
        feedbackForm.addEventListener('submit', function(e) {
          e.preventDefault();
          
          const formData = new FormData(feedbackForm);
          const feedback = formData.get('feedback_form_input');
          
          // Don't submit if empty
          if (!feedback || feedback.trim() === '') {
            feedbackInput.focus();
            return;
          }
          
          // Submit to backend
          fetch('/submit-feedback', {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
            },
            body: JSON.stringify({
              feedback_form_input: feedback
            })
          })
          .then(response => response.json())
          .then(data => {
            if (data.success) {
              // Hide the form panel
              feedbackPanel.style.display = 'none';
              feedbackPanel.setAttribute('aria-hidden', 'true');
              
              // Show thank you message
              thanksMessage.style.display = 'block';
              
              // Clear the form
              feedbackForm.reset();
              
              // After 5 seconds, reset everything
              setTimeout(function() {
                thanksMessage.style.display = 'none';
                feedbackLinkText.style.display = 'block';
              }, 5000);
            } else {
              console.error('Failed to submit feedback:', data.message);
              // Could show an error message here
            }
          })
          .catch(error => {
            console.error('Error submitting feedback:', error);
            // Could show an error message here
          });
        });
      }
  
      // Close panel when clicking outside (optional enhancement)
      document.addEventListener('click', function(e) {
        if (feedbackPanel.style.display === 'block' && 
            !feedbackPanel.contains(e.target) && 
            !feedbackLink.contains(e.target)) {
          // Don't close if clicking inside the panel
          return;
        }
      });
  
      // Escape key to close panel
      document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape' && feedbackPanel.style.display === 'block') {
          closeFeedbackPanel();
        }
      });
    }
  })
  