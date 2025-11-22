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
        "order": [[0, "desc"]],
        columns: [
            {
                data: 'id',
                width: "5%",
                render: function (data) {
                    return `<span class="fw-bold">${data}</span>`;
                }
            },
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
                render: function (data, type, row) {

                    if (!data || data.length === 0)
                        return '<span class="text-muted">N/A</span>';

                    if (data.length === 1)
                        return `<span class="fw-bold">${data[0]}</span>`;

                    const first = data[0];
                    const remaining = data.length - 1;
                    const allSerials = data.join('<br>'); // For SweetAlert

                    return `
                        <div class="d-flex align-items-center gap-2">
                            <span class="fw-bold">${first}</span>
                            <button type="button" class="btn btn-sm btn-outline-info ms-1 p-0" 
                                    style="width: 20px; height: 20px; font-size: 10px;" 
                                    onclick="showFullSerials('${allSerials}')">
                                <i class="bi bi-eye"></i>
                            </button>
                        </div>
                        <small class="text-muted">+${remaining} more serial number${remaining > 1 ? 's' : ''}</small>
                    `;
                }
            },
            {
                data: 'modelName',
                width: "12%",
                render: function (data) {
                    //if (!data || data.length === 0) return '<span class="text-muted">N/A</span>';
                    //if (data.length === 1) return data[0];

                    //const uniqueModels = [...new Set(data)];
                    //if (uniqueModels.length === 1) return uniqueModels[0];

                    //return `
                    //    <div>
                    //        <div>${uniqueModels[0]}</div>
                    //        <small class="text-muted">+${uniqueModels.length - 1} more model${uniqueModels.length > 2 ? 's' : ''}</small>
                    //    </div>
                    //`;
                    return data || 'N/A';
                }
            },
            {
                data: 'clientName',
                width: "12%",
                render: function (data) {
                    //if (!data || data.length === 0) return '<span class="text-muted">N/A</span>';
                    //if (data.length === 1) return data[0];

                    //const uniqueClients = [...new Set(data)];
                    //if (uniqueClients.length === 1) return uniqueClients[0];

                    //return `
                    //    <div>
                    //        <div>${uniqueClients[0]}</div>
                    //        <small class="text-muted">+${uniqueClients.length - 1} more client${uniqueClients.length > 2 ? 's' : ''}</small>
                    //    </div>
                    //`;
                    return data || 'N/A';
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
        dataTable.column(9).visible(true);   // show admin column
    } else {
        dataTable.column(9).visible(false);
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
        dataTable.column(8).search('').draw(); // Status column is now at index 7
    } else {
        dataTable.column(8).search('^' + status + '$', true, false).draw();
    }
}

function clearFilters() {
    const statusFilter = document.getElementById('statusFilter');
    if (statusFilter) {
        statusFilter.value = 'All';
    }
    dataTable.order([
        [0, "desc"]
    ]).draw();
    dataTable.columns().search('').draw();

    toastr.info('Filters cleared');
}

function showFullSerials(description) {
    // Allow <br>, escape everything else
    const safeDescription = description
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#39;")
        .replace(/&lt;br&gt;/g, "<br>"); // re-enable <br>

    Swal.fire({
        title: '<i class="bi bi-card-text"></i> Covered Serial Numbers',
        html: `
            <div style="text-align: left; max-height: 400px; overflow-y: auto; background: #f8f9fa; padding: 15px; border-radius: 5px; border: 1px solid #dee2e6;">
                <p style="margin: 0; white-space: normal;">${safeDescription}</p>
            </div>
        `,
        icon: 'info',
        confirmButtonText: 'Close',
        width: '400px'
    });
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