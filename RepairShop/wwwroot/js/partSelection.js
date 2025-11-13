document.addEventListener('DOMContentLoaded', function () {
    // Initialize TomSelect for dropdowns
    const categoryTomSelect = new TomSelect('#categorySelect', {
        placeholder: 'Select Category',
        create: false,
        sortField: { field: 'text', direction: 'asc' }
    });

    const partTomSelect = new TomSelect('#partSelect', {
        placeholder: 'Select Replacement Part',
        create: false,
        sortField: { field: 'text', direction: 'asc' },
        disabled: true
    });

    const statusSelect = document.getElementById('statusSelect');
    const replacementSection = document.getElementById('replacementSection');
    const partInfo = document.getElementById('partInfo');
    const partNameDisplay = document.getElementById('partNameDisplay');
    const partQuantity = document.getElementById('partQuantity');
    const partPrice = document.getElementById('partPrice');
    const selectedPartId = document.getElementById('selectedPartId');
    const clearPartBtn = document.getElementById('clearPartBtn');
    const finalStatus = document.getElementById('finalStatus');

    // Action buttons
    const completeBtn = document.getElementById('completeBtn');
    const notRepairableBtn = document.getElementById('notRepairableBtn');
    const pendingBtn = document.getElementById('pendingBtn');
    const transactionBodyForm = document.getElementById('transactionBodyForm');
    const brokenPartNameInput = document.getElementById('tbForUpsert_BrokenPartName');

    // Status buttons (Repair / Replace)
    const repairBtn = document.getElementById('repairBtn');
    const replaceBtn = document.getElementById('replaceBtn');

    function activateRepair() {
        statusSelect.value = 'PendingForRepair';
        replacementSection?.classList.add('d-none');
        resetPartInfo?.();

        // visual state
        repairBtn.classList.add('btn-success', 'text-white');
        repairBtn.classList.remove('btn-outline-success');
        replaceBtn.classList.remove('btn-warning', 'text-white');
        replaceBtn.classList.add('btn-outline-warning');
    }

    function activateReplace() {
        statusSelect.value = 'PendingForReplacement';
        replacementSection?.classList.remove('d-none');

        // visual state
        replaceBtn.classList.add('btn-warning', 'text-white');
        replaceBtn.classList.remove('btn-outline-warning');
        repairBtn.classList.remove('btn-success', 'text-white');
        repairBtn.classList.add('btn-outline-success');
    }

    if (repairBtn && replaceBtn) {
        // set default to "Repair" on page load
        activateRepair();

        // button click events
        repairBtn.addEventListener('click', activateRepair);
        replaceBtn.addEventListener('click', activateReplace);
    }
    // Show/hide replacement section based on status
    if (statusSelect) {
        statusSelect.addEventListener('change', function () {
            const status = this.value;
            if (status === 'PendingForReplacement') {
                replacementSection.classList.remove('d-none');
            } else {
                replacementSection.classList.add('d-none');
                resetPartInfo();
            }
        });
    }

    // Set initial state based on current status
    if (statusSelect && statusSelect.value === 'PendingForReplacement') {
        replacementSection.classList.remove('d-none');
    }

    // When category changes, load parts for that category
    categoryTomSelect.on('change', function (category) {
        if (category) {
            loadPartsByCategory(category);
            partTomSelect.enable();
        } else {
            partTomSelect.disable();
            partTomSelect.clear();
            resetPartInfo();
        }
    });

    // When part is selected, show part info
    partTomSelect.on('change', function (partId) {
        if (partId) {
            loadPartDetails(partId);
        } else {
            resetPartInfo();
        }
    });

    // Clear selected replacement part
    if (clearPartBtn) {
        clearPartBtn.addEventListener('click', function () {
            resetPartInfo();
            categoryTomSelect.clear();
            partTomSelect.clear();
            partTomSelect.disable();
        });
    }

    // Button click handlers
    if (completeBtn) {
        completeBtn.addEventListener('click', function () {
            //if (!validateForm()) return;

            const currentStatus = statusSelect.value;
            if (currentStatus === 'PendingForRepair') {
                finalStatus.value = 'Fixed';
            } else if (currentStatus === 'PendingForReplacement') {
                finalStatus.value = 'Replaced';
            } else {
                finalStatus.value = currentStatus;
            }

            transactionBodyForm.submit();
        });
    }

    if (notRepairableBtn) {
        notRepairableBtn.addEventListener('click', function () {
            //if (!validateForm()) return;

            const currentStatus = statusSelect.value;
            if (currentStatus === 'PendingForRepair') {
                finalStatus.value = 'NotRepairable';
            } else if (currentStatus === 'PendingForReplacement') {
                finalStatus.value = 'NotReplaceable';
            } else {
                finalStatus.value = currentStatus;
            }

            transactionBodyForm.submit();
        });
    }

    if (pendingBtn) {
        pendingBtn.addEventListener('click', function () {
            //if (!validateForm()) return;

            // Keep the original pending status
            finalStatus.value = statusSelect.value;
            transactionBodyForm.submit();
        });
    }

    function loadPartsByCategory(category) {
        fetch(`/User/TransactionBodies/Upsert?handler=PartsByCategory&category=${encodeURIComponent(category)}`)
            .then(response => response.json())
            .then(parts => {
                partTomSelect.clear();
                partTomSelect.clearOptions();

                parts.forEach(function (part) {
                    partTomSelect.addOption({
                        value: part.id,
                        text: part.name
                    });
                });
            })
            .catch(error => {
                console.error('Error loading parts:', error);
                alert('Error loading parts');
            });
    }

    function loadPartDetails(partId) {
        fetch(`/User/TransactionBodies/Upsert?handler=PartDetails&id=${partId}`)
            .then(response => response.json())
            .then(part => {
                if (part) {
                    partNameDisplay.textContent = part.name;
                    partQuantity.textContent = part.quantity;
                    partPrice.textContent = part.price || '0.00';
                    selectedPartId.value = part.id;
                    partInfo.classList.remove('d-none');
                }
            })
            .catch(error => {
                console.error('Error loading part details:', error);
                alert('Error loading part details');
            });
    }

    function resetPartInfo() {
        partInfo.classList.add('d-none');
        partNameDisplay.textContent = '';
        partQuantity.textContent = '';
        partPrice.textContent = '';
        selectedPartId.value = '';
    }

    function validateForm() {
        // Basic validation
        if (!statusSelect.value) {
            alert('Please select a status first.');
            return false;
        }

        if (!brokenPartNameInput.value.trim()) {
            alert('Please enter a broken part name.');
            return false;
        }

        // Additional validation for replacement parts
        if (statusSelect.value === 'PendingForReplacement' && !selectedPartId.value) {
            alert('Please select a replacement part for pending replacement status.');
            return false;
        }

        return true;
    }
});