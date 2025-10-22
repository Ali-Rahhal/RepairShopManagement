let dataTable;
let currentContractId = null;
let currentClientId = null;

document.addEventListener('DOMContentLoaded', function () {
    loadDataTable();
});

function isAdmin() {//function to check if the user is admin
    return document.getElementById("isAdmin").value === "True";
}

function loadDataTable() {
    dataTable = new DataTable('#tblData', {
        "stateSave": true,
        "stateDuration": 86400, // Any positive number = sessionStorage (in seconds)
        // 86400 seconds = 24 hours, but sessionStorage lasts only for the browser session
        ajax: {
            url: '/Admin/MaintenanceContracts/Index?handler=All',
            dataSrc: 'data'
        },
        dom: '<"d-flex justify-content-between align-items-center mb-2"l<"ml-auto"f>>rtip',
        columns: [
            {
                data: 'contractNumber',
                width: "10%",
                render: function (data) {
                    return `<strong>${data}</strong>`;
                }
            },
            {
                data: 'clientName',
                width: "15%",
                render: function (data) {
                    return data || 'N/A';
                }
            },
            {
                data: 'startDate',
                width: "10%",
                render: function (data) {
                    if (data == null) {
                        return 'N/A';
                    }

                    const date = new Date(data);
                    const options = {
                        day: '2-digit',
                        month: '2-digit',
                        year: 'numeric'
                    };
                    const formattedDate = date.toLocaleDateString('en-GB', options);

                    return formattedDate;
                }
            },
            {
                data: 'endDate',
                width: "10%",
                render: function (data) {
                    if (data == null) {
                        return 'N/A';
                    }

                    const date = new Date(data);
                    const options = {
                        day: '2-digit',
                        month: '2-digit',
                        year: 'numeric'
                    };
                    const formattedDate = date.toLocaleDateString('en-GB', options);

                    return formattedDate;
                }
            },
            {
                data: 'daysRemaining',
                width: "12%",
                render: function (data, type, row) {
                    if (row.isExpired) {
                        return '<span class="badge bg-danger p-2 fs-5">Expired</span>';
                    } else if (data < 30) {
                        return `<span class="badge bg-warning p-2 fs-5">${data} days</span>`;
                    } else {
                        return `<span class="badge bg-success p-2 fs-5">${data} days</span>`;
                    }
                }
            },
            {
                data: 'status',
                width: "8%",
                render: function (data) {
                    if (data === 'Active') {
                        return '<span class="badge bg-success p-2 fs-5">Active</span>';
                    } else {
                        return '<span class="badge bg-danger p-2 fs-5">Expired</span>';
                    }
                }
            },
            {
                data: 'id',//visible for admins only
                render: function (data, type, row) {
                    return `<div class="w-100 d-flex justify-content-center" role="group">
                        <a href="/Admin/MaintenanceContracts/Upsert?id=${data}" title="Edit" class="btn btn-primary mx-2">
                            <i class="bi bi-pencil-square"></i>
                        </a>
                        <button onclick="showAssignContractModal(${data}, ${row.clientId})" title="Manage Serial Numbers" class="btn btn-info mx-2">
                            <i class="bi bi-link-45deg"></i>
                        </button>
                        <a onclick="Delete('/Admin/MaintenanceContracts/Index?handler=Delete&id=${data}')" title="Delete" class="btn btn-danger mx-2">
                            <i class="bi bi-trash-fill"></i>
                        </a>
                    </div>`;
                },
                width: "18%",
                orderable: false
            }
        ],
        language: {
            emptyTable: "No maintenance contracts found",
            zeroRecords: "No matching contracts found"
        },
        order: [[0, 'desc']] // Sort by contract number descending
    });

    if (isAdmin()) {
        dataTable.column(6).visible(true);   // show admin column
    } else {
        dataTable.column(6).visible(false);
    }

    // Add event listener for status filter
    const statusFilter = document.getElementById('statusFilter');
    if (statusFilter) {
        statusFilter.addEventListener('change', applyStatusFilter);
    }

    // Add event listener for select all checkbox
    document.getElementById('selectAllSerialNumbers').addEventListener('change', function () {
        const checkboxes = document.querySelectorAll('.serial-number-checkbox');
        checkboxes.forEach(checkbox => {
            checkbox.checked = this.checked;
        });
        updateSelectionCount();
    });
}

function applyStatusFilter() {
    const status = document.getElementById('statusFilter').value;

    if (status === 'All') {
        dataTable.column(5).search('').draw(); // Status column (index 5)
    } else {
        dataTable.column(5).search('^' + status + '$', true, false).draw();
    }
}

function clearFilters() {
    const statusFilter = document.getElementById('statusFilter');
    if (statusFilter) {
        statusFilter.value = 'All';
    }
    dataTable.columns().search('').draw();

    toastr.info('Filters cleared');
}

function showAssignContractModal(contractId, clientId) {
    currentContractId = contractId;
    currentClientId = clientId;

    // Show loading state
    document.getElementById('modalLoading').style.display = 'block';
    document.getElementById('modalContent').style.display = 'none';

    // Fetch serial numbers for this client (both assigned and available)
    fetch(`/Admin/MaintenanceContracts/Index?handler=ClientSerialNumbers&contractId=${contractId}&clientId=${clientId}`)
        .then(response => response.json())
        .then(data => {
            document.getElementById('modalLoading').style.display = 'none';
            document.getElementById('modalContent').style.display = 'block';

            const serialNumbersList = document.getElementById('serialNumbersList');
            serialNumbersList.innerHTML = '';

            // Update summary badges
            document.getElementById('assignedCount').textContent = `${data.assignedCount} assigned`;
            document.getElementById('availableCount').textContent = `${data.availableCount} available`;

            if (data.serialNumbers && data.serialNumbers.length > 0) {
                let assignedSectionAdded = false;
                let availableSectionAdded = false;

                data.serialNumbers.forEach(sn => {
                    // Add section headers
                    if (sn.isAssigned && !assignedSectionAdded) {
                        const assignedHeader = document.createElement('div');
                        assignedHeader.className = 'fw-bold text-success mt-3 mb-2';
                        assignedHeader.innerHTML = '<i class="bi bi-check-circle"></i> Currently Assigned';
                        serialNumbersList.appendChild(assignedHeader);
                        assignedSectionAdded = true;
                    } else if (!sn.isAssigned && !availableSectionAdded) {
                        const availableHeader = document.createElement('div');
                        availableHeader.className = 'fw-bold text-secondary mt-3 mb-2';
                        availableHeader.innerHTML = '<i class="bi bi-circle"></i> Available for Assignment';
                        serialNumbersList.appendChild(availableHeader);
                        availableSectionAdded = true;
                    }

                    const checkboxDiv = document.createElement('div');
                    checkboxDiv.className = `ms-1 form-check`;
                    checkboxDiv.innerHTML = `
                        <input class="form-check-input serial-number-checkbox" type="checkbox" 
                               value="${sn.id}" id="sn-${sn.id}" ${sn.isAssigned ? 'checked' : ''}>
                        <label class="form-check-label" for="sn-${sn.id}">
                            <strong>${sn.value}</strong> - ${sn.modelName}
                            ${sn.isAssigned ? '<span class="badge bg-success ms-2">Assigned</span>' : ''}
                        </label>
                    `;
                    serialNumbersList.appendChild(checkboxDiv);
                });

                // Add change event listeners to update select all state
                const checkboxes = document.querySelectorAll('.serial-number-checkbox');
                checkboxes.forEach(checkbox => {
                    checkbox.addEventListener('change', updateSelectionCount);
                });

                updateSelectionCount();
            } else {
                serialNumbersList.innerHTML = '<div class="alert alert-warning">No serial numbers found for this client.(May be assigned to other contracts)</div>';
            }

            // Show modal
            const modal = new bootstrap.Modal(document.getElementById('assignContractModal'));
            modal.show();
        })
        .catch(error => {
            console.error('Error:', error);
            document.getElementById('modalLoading').style.display = 'none';
            toastr.error('Error loading serial numbers');
        });
}

function updateSelectionCount() {
    const checkboxes = document.querySelectorAll('.serial-number-checkbox');
    const checkedBoxes = document.querySelectorAll('.serial-number-checkbox:checked');

    // Update select all checkbox state
    const selectAll = document.getElementById('selectAllSerialNumbers');
    selectAll.checked = checkedBoxes.length === checkboxes.length;
    selectAll.indeterminate = checkedBoxes.length > 0 && checkedBoxes.length < checkboxes.length;

    // Update summary
    document.getElementById('assignedCount').textContent = `${checkedBoxes.length} selected`;
}

function assignContractToSelected() {
    const selectedSerialNumbers = Array.from(document.querySelectorAll('.serial-number-checkbox:checked'))
        .map(checkbox => parseInt(checkbox.value));

    // Create form data for POST request
    const formData = new FormData();
    formData.append('contractId', currentContractId);
    selectedSerialNumbers.forEach(id => formData.append('serialNumberIds', id));

    fetch('/Admin/MaintenanceContracts/Index?handler=AssignToSerialNumbers', {
        method: 'POST',
        body: formData,
        headers: {
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
        }
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                toastr.success(data.message);
                // Close modal
                const modal = bootstrap.Modal.getInstance(document.getElementById('assignContractModal'));
                modal.hide();

                // Optional: Refresh the table if needed
                // dataTable.ajax.reload();
            } else {
                toastr.error(data.message);
            }
        })
        .catch(error => {
            console.error('Error:', error);
            toastr.error('Error updating serial numbers');
        });
}

function Delete(url) {
    Swal.fire({
        title: "Are you sure you want to delete this maintenance contract?",
        text: "This action cannot be undone!",
        icon: "warning",
        showCancelButton: true,
        confirmButtonColor: "#3085d6",
        cancelButtonColor: "#d33",
        confirmButtonText: "Yes, delete it!"
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: url,
                type: 'GET',
                success: function (data) {
                    if (data.success) {
                        dataTable.ajax.reload();
                        toastr.success(data.message);
                    }
                    else {
                        toastr.error(data.message);
                    }
                },
                error: function () {
                    toastr.error('An error occurred while deleting the model');
                }
            });
        }
    });
}