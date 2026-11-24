// BuildSure client-side behaviour (jQuery).
// Everything here is "progressive enhancement": the pages still work with plain form posts
// if JavaScript is disabled - jQuery just makes them nicer.
$(function () {

    // ------------------------------------------------------------------
    // Helper: show a Bootstrap alert in the #ajax-alert placeholder (_Alerts.cshtml)
    // ------------------------------------------------------------------
    function showAlert(type, message) {
        var $alert = $('<div class="alert alert-dismissible fade show" role="alert"></div>')
            .addClass('alert-' + type)
            .text(message) // .text() escapes HTML, so server messages cannot inject markup
            .append('<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>');
        $('main > .alert').remove(); // hide older TempData messages
        $('#ajax-alert').empty().append($alert);
        $('html, body').animate({ scrollTop: 0 }, 200);
    }

    // ------------------------------------------------------------------
    // 1. Confirmation dialog for any <form class="js-confirm" data-confirm="...">
    // ------------------------------------------------------------------
    var confirmModal = document.getElementById('confirmModal')
        ? new bootstrap.Modal('#confirmModal')
        : null;
    var $pendingForm = null;

    $(document).on('submit', 'form.js-confirm', function (e) {
        var $form = $(this);
        if ($form.data('confirmed')) {           // user already clicked "Yes": let the submit continue
            $form.data('confirmed', false);
            return;
        }
        e.preventDefault();
        e.stopImmediatePropagation();            // stop other submit handlers (e.g. AJAX review) for now
        $pendingForm = $form;
        $('#confirmModalMessage').text($form.data('confirm') || 'Are you sure?');
        $('#confirmModalOk')
            .text($form.data('confirm-button') || 'Yes, continue')
            .attr('class', 'btn ' + ($form.data('confirm-class') || 'btn-danger'));
        confirmModal.show();
    });

    $('#confirmModalOk').on('click', function () {
        confirmModal.hide();
        if ($pendingForm) {
            $pendingForm.data('confirmed', true);
            $pendingForm.trigger('submit');      // runs the remaining handlers, then the real submit
            $pendingForm = null;
        }
    });

    // ------------------------------------------------------------------
    // 2. AJAX approve / reject on the application Details page
    //    POST with the anti-forgery token -> JSON -> update the page without reloading
    // ------------------------------------------------------------------
    $(document).on('submit', 'form.js-review', function (e) {
        e.preventDefault();
        var $form = $(this);
        var $panel = $('#review-panel');

        if ($form.data('require-reason') && !$.trim($form.find('[name="reason"]').val())) {
            $form.find('[name="reason"]').addClass('is-invalid').trigger('focus');
            return;
        }

        $panel.addClass('is-loading');
        $.ajax({
            url: $form.attr('action'),
            type: 'POST',
            data: $form.serialize(),                         // includes __RequestVerificationToken
            headers: { 'X-Requested-With': 'XMLHttpRequest' } // tells the controller to return JSON
        })
            .done(function (result) {
                // Dynamic status update
                $('.js-status-badge')
                    .removeClass('text-bg-warning text-bg-success text-bg-danger')
                    .addClass(result.badgeCss)
                    .text(result.status);
                $('#reviewed-at').text(result.reviewedAt || '-');
                $('#review-note').text(result.reviewNote || '-');
                if (result.certificateNumber) {
                    $('#certificate-number').text(result.certificateNumber);
                    $('#certificate-row').removeClass('d-none');
                }
                $('.js-pending-only').remove();               // Edit/Review are no longer allowed
                if (result.status === 'Approved') { $('.js-not-approved').remove(); } // cannot delete approved
                $panel.remove();
                showAlert('success', result.message);
            })
            .fail(function (xhr) {
                var message = xhr.responseJSON && xhr.responseJSON.message
                    ? xhr.responseJSON.message
                    : 'Something went wrong (' + xhr.status + ').';
                showAlert('danger', message);
            })
            .always(function () { $panel.removeClass('is-loading'); });
    });

    $(document).on('input', '[name="reason"]', function () { $(this).removeClass('is-invalid'); });

    // ------------------------------------------------------------------
    // 3. Live search / status filter on the Applications list (AJAX partial view)
    // ------------------------------------------------------------------
    var $filter = $('#application-filter');
    var searchTimer = null;

    function loadApplications() {
        var query = $filter.serialize();
        var $results = $('#application-results').addClass('is-loading');
        $.ajax({ url: $filter.attr('action'), data: query, headers: { 'X-Requested-With': 'XMLHttpRequest' } })
            .done(function (html) {
                $results.html(html);
                history.replaceState(null, '', $filter.attr('action') + (query ? '?' + query : ''));
            })
            .fail(function () { showAlert('danger', 'Could not load applications.'); })
            .always(function () { $results.removeClass('is-loading'); });
    }

    $filter.on('submit', function (e) { e.preventDefault(); loadApplications(); });
    $filter.on('change', 'select', loadApplications);
    $filter.on('input', 'input[name="search"]', function () {
        clearTimeout(searchTimer);
        searchTimer = setTimeout(loadApplications, 300); // debounce: wait until the user stops typing
    });

    // ------------------------------------------------------------------
    // 4. Dashboard "Refresh" button: GET JSON and update the numbers
    // ------------------------------------------------------------------
    $('#refresh-stats').on('click', function () {
        var $btn = $(this).prop('disabled', true);
        $.getJSON($btn.data('url'))
            .done(function (stats) {
                $.each(stats, function (key, value) { $('[data-stat="' + key + '"]').text(value); });
                $('#stats-updated').text('Updated ' + new Date().toLocaleTimeString());
            })
            .fail(function () { showAlert('danger', 'Could not refresh statistics.'); })
            .always(function () { $btn.prop('disabled', false); });
    });

    // ------------------------------------------------------------------
    // 5. Small client-side interaction: live character counter for textareas
    //    <textarea maxlength="1000" data-counter="#notes-counter">
    // ------------------------------------------------------------------
    $('textarea[data-counter]').each(function () {
        var $area = $(this);
        var $counter = $($area.data('counter'));
        var max = parseInt($area.attr('maxlength'), 10);
        function update() { $counter.text($area.val().length + ' / ' + max); }
        $area.on('input', update);
        update();
    });
});
