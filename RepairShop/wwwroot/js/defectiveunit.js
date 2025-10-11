let dataTable;

$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        stateSave: true,
        ajax: {
            url: '/Admin/DefectiveUnits/Index?handler=All',
            dataSrc: 'data'
        },
        dom: '<"d-flex justify-content-between align-items-center mb-2"l<"ml-auto"f>>rtip',
        columns: [
            {
                data: 'serialNumber',
                width: "10%",
                render: function (data) {
                    return `<strong>${data}</strong>`;
                }
            },
            {
                data: 'modelName',
                width: "12%",
                render: function (data) {
                    return data || 'N/A';
                }
            },
            {
                data: 'clientName',
                width: "12%",
                render: function (data) {
                    return data || 'N/A';
                }
            },
            {
                data: 'reportedDate',
                width: "8%",
                render: function (data) {
                    return data ? new Date(data).toLocaleDateString() : 'N/A';
                }
            },
            {
                data: 'description',
                width: "20%",
                render: function (data) {
                    return data || 'No description';
                }
            },
            {
                data: 'status',
                width: "10%",
                render: function (data) {
                    let badgeClass = 'bg-secondary';
                    switch (data) {
                        case 'Reported': badgeClass = 'bg-info'; break;
                        case 'UnderRepair': badgeClass = 'bg-warning'; break;
                        case 'Fixed': badgeClass = 'bg-success'; break;
                        case 'OutOfService': badgeClass = 'bg-danger'; break;
                    }
                    return `<span class="badge ${badgeClass}">${data}</span>`;
                }
            },
            {
                data: 'daysSinceReported',
                width: "8%",
                render: function (data) {
                    if (data === 0) return '<span class="badge bg-info">Today</span>';
                    if (data === 1) return '<span class="badge bg-info">1 day</span>';
                    if (data < 7) return `<span class="badge bg-info">${data} days</span>`;
                    if (data < 30) return `<span class="badge bg-warning">${data} days</span>`;
                    return `<span class="badge bg-danger">${data} days</span>`;
                }
            },
            {
                data: 'coverage',
                width: "8%",
                render: function (data, type, row) {
                    let coverageHtml = '';
                    if (row.warrantyCovered === 'Yes') {
                        coverageHtml += '<span class="badge bg-success me-1" title="Covered by warranty">W</span>';
                    }
                    if (row.contractCovered === 'Yes') {
                        coverageHtml += '<span class="badge bg-info" title="Covered by maintenance contract">C</span>';
                    }
                    return coverageHtml || '<span class="text-muted">-</span>';
                }
            },
            {
                data: 'resolvedDate',
                width: "8%",
                render: function (data) {
                    if (data === 'Not resolved') {
                        return '<span class="badge bg-warning">Pending</span>';
                    }
                    return data ? new Date(data).toLocaleDateString() : 'N/A';
                }
            },
            {
                data: 'id',
                render: function (data, type, row) {
                    let statusButtons = '';

                    if (row.status !== 'Fixed' && row.status !== 'OutOfService') {
                        if (row.status === 'Reported') {
                            statusButtons = `
                                <button onclick="updateStatus(${data}, 'UnderRepair')" class="btn btn-warning btn-sm mx-1" title="Mark as Under Repair">
                                    <i class="bi bi-tools"></i>
                                </button>
                            `;
                        } else if (row.status === 'UnderRepair') {
                            statusButtons = `
                                <button onclick="updateStatus(${data}, 'Fixed')" class="btn btn-success btn-sm mx-1" title="Mark as Fixed">
                                    <i class="bi bi-check-circle"></i>
                                </button>
                                <button onclick="updateStatus(${data}, 'OutOfService')" class="btn btn-danger btn-sm mx-1" title="Mark as Out of Service">
                                    <i class="bi bi-exclamation-triangle"></i>
                                </button>
                            `;
                        }
                    }

                    return `<div class="d-flex justify-content-center" role="group">
                        <a href="/Admin/DefectiveUnits/Upsert?id=${data}" title="Edit" class="btn btn-primary btn-sm mx-1">
                            <i class="bi bi-pencil-square"></i>
                        </a>
                        ${statusButtons}
                        <button onclick="Delete(${data})" title="Delete" class="btn btn-danger btn-sm mx-1">
                            <i class="bi bi-trash-fill"></i>
                        </button>
                    </div>`;
                },
                width: "12%",
                orderable: false
            }
        ],
        language: {
            emptyTable: "No defective units found",
            zeroRecords: "No matching defective units found"
        },
        order: [[3, 'desc']] // Sort by reported date descending
    });

    // Add event listeners for filters
    $('#statusFilter').on('change', function () {
        applyFilters();
    });

    $('#resolvedFilter').on('change', function () {
        applyFilters();
    });
}

function applyFilters() {
    const statusFilter = $('#statusFilter').val();
    const resolvedFilter = $('#resolvedFilter').val();

    // Clear all column searches first
    dataTable.columns().search('');

    // Apply status filter
    if (statusFilter !== 'All') {
        dataTable.column(5).search('^' + statusFilter + '$', true, false); // Status column
    }

    // Apply resolved filter
    if (resolvedFilter !== 'All') {
        if (resolvedFilter === 'Resolved') {
            dataTable.column(8).search('^(?!.*Pending).*$', true, false); // Not "Pending" in resolved date
        } else if (resolvedFilter === 'Unresolved') {
            dataTable.column(8).search('^Pending$', true, false); // "Pending" in resolved date
        }
    }

    dataTable.draw();
}

function clearFilters() {
    $('#statusFilter').val('All');
    $('#resolvedFilter').val('All');
    dataTable.columns().search('').draw();

    toastr.info('All filters cleared');
}

function updateStatus(id, newStatus) {
    const statusText = newStatus === 'UnderRepair' ? 'Under Repair' :
        newStatus === 'OutOfService' ? 'Out of Service' : newStatus;

    Swal.fire({
        title: `Update status to ${statusText}?`,
        text: "This will update the defective unit status.",
        icon: "question",
        showCancelButton: true,
        confirmButtonColor: "#3085d6",
        cancelButtonColor: "#d33",
        confirmButtonText: "Yes, update it!"
    }).then((result) => {
        if (result.isConfirmed) {
            fetch(`/Admin/DefectiveUnits/Index?handler=UpdateStatus&id=${id}&status=${newStatus}`)
                .then(response => response.json())
                .then(data => {
                    if (data.success) {
                        dataTable.ajax.reload();
                        toastr.success(data.message);
                    } else {
                        toastr.error(data.message);
                    }
                })
                .catch(error => {
                    console.error('Error:', error);
                    toastr.error('Error updating status');
                });
        }
    });
}

function Delete(id) {
    Swal.fire({
        title: "Are you sure you want to delete this defective unit report?",
        text: "This action cannot be undone!",
        icon: "warning",
        showCancelButton: true,
        confirmButtonColor: "#3085d6",
        cancelButtonColor: "#d33",
        confirmButtonText: "Yes, delete it!"
    }).then((result) => {
        if (result.isConfirmed) {
            fetch(`/Admin/DefectiveUnits/Index?handler=Delete&id=${id}`)
                .then(response => response.json())
                .then(data => {
                    if (data.success) {
                        dataTable.ajax.reload();
                        toastr.success(data.message);
                    } else {
                        toastr.error(data.message);
                    }
                })
                .catch(error => {
                    console.error('Error:', error);
                    toastr.error('Error deleting defective unit report');
                });
        }
    });
}