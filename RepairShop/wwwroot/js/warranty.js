let dataTable;

document.addEventListener('DOMContentLoaded', function () {
    loadDataTable();
});

function isAdmin() {
    return document.getElementById("isAdmin").value === "True";
}

function loadDataTable() {
    dataTable = new DataTable('#tblData', {
        "stateSave": true,
        "stateDuration": 86400,
        ajax: {
            url: '/Admin/Warranties/Index?handler=All',
            dataSrc: 'data'
        },
        dom: '<"d-flex justify-content-between align-items-center mb-2"l<"ml-auto"f>>rtip',
        columns: [
            {
                data: 'coveredCount',
                width: "8%",
                render: function (data, type, row) {
                    return `<span class="badge bg-info p-2 fs-6">${data} items</span>`;
                }
            },
            {
                data: 'serialNumbers',
                width: "15%",
                render: function (data) {
                    if (!data || data.length === 0) return '<span class="text-muted">N/A</span>';
                    if (data.length === 1) return `<span class="fw-bold">${data[0]}</span>`;

                    const firstItem = data[0];
                    const remainingCount = data.length - 1;
                    return `
                        <div>
                            <div class="fw-bold">${firstItem}</div>
                            <small class="text-muted">+${remainingCount} more serial number${remainingCount > 1 ? 's' : ''}</small>
                        </div>
                    `;
                }
            },
            {
                data: 'modelNames',
                width: "12%",
                render: function (data) {
                    if (!data || data.length === 0) return '<span class="text-muted">N/A</span>';
                    if (data.length === 1) return data[0];

                    const uniqueModels = [...new Set(data)];
                    if (uniqueModels.length === 1) return uniqueModels[0];

                    return `
                        <div>
                            <div>${uniqueModels[0]}</div>
                            <small class="text-muted">+${uniqueModels.length - 1} more model${uniqueModels.length > 2 ? 's' : ''}</small>
                        </div>
                    `;
                }
            },
            {
                data: 'clientNames',
                width: "12%",
                render: function (data) {
                    if (!data || data.length === 0) return '<span class="text-muted">N/A</span>';
                    if (data.length === 1) return data[0];

                    const uniqueClients = [...new Set(data)];
                    if (uniqueClients.length === 1) return uniqueClients[0];

                    return `
                        <div>
                            <div>${uniqueClients[0]}</div>
                            <small class="text-muted">+${uniqueClients.length - 1} more client${uniqueClients.length > 2 ? 's' : ''}</small>
                        </div>
                    `;
                }
            },
            {
                data: 'startDate',
                width: "10%",
                render: function (data) {
                    if (!data) return '<span class="text-muted">N/A</span>';
                    const date = new Date(data);
                    const options = { day: '2-digit', month: '2-digit', year: 'numeric' };
                    const formattedDate = date.toLocaleDateString('en-GB', options);
                    return formattedDate;
                }
            },
            {
                data: 'endDate',
                width: "10%",
                render: function (data) {
                    if (!data) return '<span class="text-muted">N/A</span>';
                    const date = new Date(data);
                    const options = { day: '2-digit', month: '2-digit', year: 'numeric' };
                    const formattedDate = date.toLocaleDateString('en-GB', options);
                    return formattedDate;
                }
            },
            {
                data: 'daysRemaining',
                width: "12%",
                render: function (data, type, row) {
                    if (row.isExpired) {
                        return '<span class="badge bg-danger p-2 fs-6">Expired</span>';
                    } else if (data < 30) {
                        return `<span class="badge bg-warning p-2 fs-6">${data} days</span>`;
                    } else {
                        return `<span class="badge bg-success p-2 fs-6">${data} days</span>`;
                    }
                }
            },
            {
                data: 'status',
                width: "8%",
                render: function (data) {
                    if (data === 'Active') {
                        return '<span class="badge bg-success p-2 fs-6">Active</span>';
                    } else {
                        return '<span class="badge bg-danger p-2 fs-6">Expired</span>';
                    }
                }
            },
            {
                data: 'id',
                render: function (data, type, row) {
                    return `<div class="w-100 d-flex justify-content-center" role="group">
                        <a href="/Admin/Warranties/Upsert?id=${data}" title="Edit" class="btn btn-primary mx-1">
                            <i class="bi bi-pencil-square"></i>
                        </a>
                        <a onclick="Delete('/Admin/Warranties/Index?handler=Delete&id=${data}')" title="Delete" class="btn btn-danger mx-1">
                            <i class="bi bi-trash-fill"></i>
                        </a>
                    </div>`;
                },
                width: "8%",
                orderable: false
            }
        ],
        language: {
            emptyTable: "No warranties found",
            zeroRecords: "No matching warranties found"
        }
    });

    if (isAdmin()) {
        dataTable.column(8).visible(true);   // show admin column
    } else {
        dataTable.column(8).visible(false);
    }

    // Add event listener for status filter
    const statusFilter = document.getElementById('statusFilter');
    if (statusFilter) {
        statusFilter.addEventListener('change', applyStatusFilter);
    }
}

function applyStatusFilter() {
    const status = document.getElementById('statusFilter').value;

    if (status === 'All') {
        dataTable.column(7).search('').draw(); // Status column is now at index 7
    } else {
        dataTable.column(7).search('^' + status + '$', true, false).draw();
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

function Delete(url) {
    Swal.fire({
        title: "Are you sure you want to delete this warranty?",
        html: "<p style='color: #dc3545; font-weight: 500;'>This will remove coverage from all associated serial numbers!</p>",
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
                    toastr.error('An error occurred while deleting the warranty');
                }
            });
        }
    });
}