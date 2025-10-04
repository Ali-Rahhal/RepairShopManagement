$(document).ready(function () {
    const statusSelect = $('#statusSelect');
    const replacementSection = $('#replacementSection');
    const categorySelect = $('#categorySelect');
    const partSelect = $('#partSelect');
    const partInfo = $('#partInfo');
    const partNameDisplay = $('#partNameDisplay');
    const partQuantity = $('#partQuantity');
    const partPrice = $('#partPrice');
    const selectedPartId = $('#selectedPartId');
    const clearPartBtn = $('#clearPartBtn');

    // Show/hide replacement section based on status
    statusSelect.change(function () {
        const status = $(this).val();
        if (status === 'PendingForReplacement') {
            replacementSection.removeClass('d-none');
        } else {
            replacementSection.addClass('d-none');
            resetPartInfo();
        }
    });

    // Set initial state based on current status
    if (statusSelect.val() === 'PendingForReplacement') {
        replacementSection.removeClass('d-none');
    }

    // When category changes, load parts for that category
    categorySelect.change(function () {
        const category = $(this).val();
        if (category) {
            loadPartsByCategory(category);
            partSelect.prop('disabled', false);
        } else {
            partSelect.prop('disabled', true).html('<option value="">Select Replacement Part</option>');
            resetPartInfo();
        }
    });

    // When part is selected, show part info
    partSelect.change(function () {
        const partId = $(this).val();
        if (partId) {
            loadPartDetails(partId);
        } else {
            resetPartInfo();
        }
    });

    // Clear selected replacement part
    clearPartBtn.click(function () {
        resetPartInfo();
        categorySelect.val('');
        partSelect.val('').prop('disabled', true);
    });

    function loadPartsByCategory(category) {
        $.get(`/User/TransactionBodies/Upsert?handler=PartsByCategory&category=${encodeURIComponent(category)}`)
            .done(function (parts) {
                partSelect.html('<option value="">Select Replacement Part</option>');
                parts.forEach(function (part) {
                    partSelect.append(new Option(part.name, part.id));
                });
            })
            .fail(function () {
                alert('Error loading parts');
            });
    }

    function loadPartDetails(partId) {
        $.get(`/User/TransactionBodies/Upsert?handler=PartDetails&id=${partId}`)
            .done(function (part) {
                if (part) {
                    partNameDisplay.text(part.name);
                    partQuantity.text(part.quantity);
                    partPrice.text(part.price || '0.00');
                    selectedPartId.val(part.id);
                    partInfo.removeClass('d-none');
                }
            })
            .fail(function () {
                alert('Error loading part details');
            });
    }

    function resetPartInfo() {
        partInfo.addClass('d-none');
        partNameDisplay.text('');
        partQuantity.text('');
        partPrice.text('');
        selectedPartId.val('');
        partSelect.val('');
    }
});