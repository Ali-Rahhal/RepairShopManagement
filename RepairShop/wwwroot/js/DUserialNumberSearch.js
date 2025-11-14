let selectedSerialId = 0;
let isCreatingNew = false;

function searchSerialNumbers() {
    const searchTerm = document.getElementById('serialNumberSearch').value.trim();
    if (searchTerm.length < 1) {
        toastr.warning('Please enter a serial number to search');
        return;
    }

    const resultsDiv = document.getElementById('searchResults');
    const resultsList = document.getElementById('searchResultsList');
    const messageDiv = document.getElementById('searchMessage');

    resultsList.innerHTML = '<div class="text-center p-3"><div class="spinner-border" role="status"></div></div>';
    resultsDiv.style.display = 'block';
    messageDiv.style.display = 'none';

    fetch(`/Admin/DefectiveUnits/Upsert?handler=SearchSerialNumber&searchTerm=${encodeURIComponent(searchTerm)}`)
        .then(response => response.json())
        .then(data => {
            resultsList.innerHTML = '';

            if (!data || Object.keys(data).length === 0) {//!data → catches null || Object.keys(data).length === 0 → catches empty { }
                // No existing serial number found - show creation fields
                showNewSerialFields(searchTerm);
                messageDiv.innerHTML = '<span class="text-warning"><i class="bi bi-info-circle"></i> Serial number not found. You can create a new one below.</span>';
                messageDiv.style.display = 'block';
                resultsDiv.style.display = 'none';
            } else {
                // Show search result
                const listItem = document.createElement('button');
                listItem.type = 'button';
                listItem.className = 'list-group-item list-group-item-action';
                listItem.innerHTML = `
                    <div class="d-flex justify-content-between align-items-start">
                        <div>
                            <strong>${data.value}</strong><br>
                            <small class="text-muted">Model: ${data.modelName} | Client: ${data.clientName} | Received: ${data.receivedDate}</small>
                        </div>
                        <div class="text-end">
                            ${data.hasWarranty ? '<span class="badge bg-success me-1">Warranty</span>' : ''}
                            ${data.hasContract ? '<span class="badge bg-info">Contract</span>' : ''}
                        </div>
                    </div>`;
                listItem.onclick = function () {
                    selectSerialNumber(data.id);
                };
                resultsList.appendChild(listItem);

                // Automatically select the first result
                selectSerialNumber(data.id);
            }
        })
        .catch(error => {
            console.error('Error:', error);
            resultsList.innerHTML = '<div class="text-center p-3 text-danger">Error loading results</div>';
        });
}

function showNewSerialFields(searchTerm) {
    // Hide existing serial info
    document.getElementById('selectedSerialInfo').style.display = 'none';

    // Show new serial fields
    const newSection = document.getElementById('newSerialFields');
    newSection.style.display = 'block';

    // Pre-fill the serial number value
    document.getElementById('newSerialValue').value = searchTerm;

    // Clear any existing serial selection
    document.getElementById('selectedSerialId').value = '0';
    selectedSerialId = 0;
    isCreatingNew = true;

    // Enable submit button
    enableSubmitButton();

    // Focus the model dropdown once it becomes visible
    setTimeout(() => {
        const modelSelect = document.getElementById('newSerialModel');
        const ts = modelSelect.tomselect;
        if (ts) {
            ts.focus();
            ts.open(); // Automatically show dropdown
            const wrapper = ts.wrapper;
            wrapper.classList.add('tomselect-focused');
        }
    }, 100);
}

function selectSerialNumber(serialNumberId) {
    document.getElementById('searchResults').style.display = 'none';
    document.getElementById('newSerialFields').style.display = 'none';
    // Clear new serial number fields to avoid validation issues
    document.getElementById('newSerialValue').value = '';

    loadSerialNumberDetails(serialNumberId);
    isCreatingNew = false;
}

function loadSerialNumberDetails(serialNumberId) {
    const infoDiv = document.getElementById('selectedSerialInfo');
    infoDiv.style.display = 'block';

    fetch(`/Admin/DefectiveUnits/Upsert?handler=SerialNumberDetails&id=${serialNumberId}`)
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                document.getElementById('selectedSerialValue').textContent = data.serialNumberValue;
                document.getElementById('selectedModelName').textContent = data.modelName;
                document.getElementById('selectedClientName').textContent = data.clientName;
                document.getElementById('SelectedReceivedDate').textContent = data.receivedDate;

                // Update warranty status
                const warrantyBadge = document.getElementById('warrantyStatus');
                if (data.hasActiveWarranty) {
                    warrantyBadge.textContent = 'Active';
                    warrantyBadge.className = 'badge bg-success';
                } else {
                    warrantyBadge.textContent = 'No Active Warranty';
                    warrantyBadge.className = 'badge bg-secondary';
                }

                // Update contract status
                const contractBadge = document.getElementById('contractStatus');
                if (data.hasActiveContract) {
                    contractBadge.textContent = 'Active';
                    contractBadge.className = 'badge bg-info';
                } else {
                    contractBadge.textContent = 'No Active Contract';
                    contractBadge.className = 'badge bg-secondary';
                }

                // Set the serial number ID
                document.getElementById('selectedSerialId').value = data.serialNumberId;
                selectedSerialId = data.serialNumberId;

                enableSubmitButton();
            } else {
                toastr.error('Error loading serial number details');
            }
        })
        .catch(error => {
            console.error('Error:', error);
            toastr.error('Error loading serial number details');
        });
}

function loadContractsForClient(clientId) {
    const selectEl = document.getElementById('newSerialContract');
    const ts = selectEl?.tomselect;

    if (!ts) {
        console.error("TomSelect instance not found for #newSerialContract");
        return;
    }

    // Clear previous options and show loading
    ts.clearOptions();
    ts.clear();
    ts.addOption({ value: "", text: "Loading..." });
    ts.refreshOptions(false);

    fetch(`/Admin/DefectiveUnits/Upsert?handler=ContractsByClient&clientId=${clientId}`)
        .then(response => response.json())
        .then(data => {
            ts.clearOptions();
            ts.addOption({ value: "", text: "--Select Contract--" });

            data.forEach(contract => {
                ts.addOption({ value: contract.id, text: contract.text });
            });

            ts.refreshOptions(false);
        })
        .catch(error => {
            console.error('Error:', error);
            ts.clearOptions();
            ts.addOption({ value: "", text: "Error loading contracts" });
            ts.refreshOptions(false);
        });
}

function enableSubmitButton() {
    const submitBtn = document.getElementById('submitBtn');
    if (submitBtn) {
        submitBtn.disabled = false;
    }
}

function disableSubmitButton() {
    const submitBtn = document.getElementById('submitBtn');
    if (submitBtn) {
        submitBtn.disabled = true;
    }
}