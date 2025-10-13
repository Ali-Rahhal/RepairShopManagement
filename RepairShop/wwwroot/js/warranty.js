let dataTable;

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
            url: '/Admin/Warranties/Index?handler=All',
            dataSrc: 'data'
        },
        dom: '<"d-flex justify-content-between align-items-center mb-2"l<"ml-auto"f>>rtip',
        columns: [
            {
                data: 'serialNumber',
                width: "15%",
                render: function (data) {
                    return data || 'N/A';
                }
            },
            {
                data: 'modelName',
                width: "15%",
                render: function (data) {
                    return data || 'N/A';
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
                width: "12%",
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
                width: "12%",
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
                    if (row.isExpired) {//isExpired is defined in the json sent from index
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
                width: "9%",
                render: function (data) {
                    if (data === 'Active') {
                        return '<span class="badge bg-success p-2 fs-5">Active</span>';
                    } else {
                        return '<span class="badge bg-danger p-2 fs-5">Expired</span>';
                    }
                }
            },
            {
                data: 'id',
                render: function (data) {
                    return `<div class="w-100 d-flex justify-content-center" role="group">
                        <a href="/Admin/Warranties/Upsert?id=${data}" title="Edit" class="btn btn-primary mx-2">
                            <i class="bi bi-pencil-square"></i>
                        </a>
                        <a onclick="Delete('/Admin/Warranties/Index?handler=Delete&id=${data}')" title="Delete" class="btn btn-danger mx-2">
                            <i class="bi bi-trash-fill"></i>
                        </a>
                    </div>`;
                },
                width: "10%",
                orderable: false
            }
        ],
        language: {
            emptyTable: "No warranties found",
            zeroRecords: "No matching warranties found"
        }
    });

    if (isAdmin()) {
        dataTable.column(7).visible(true);   // show admin column
    } else {
        dataTable.column(7).visible(false);
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
        dataTable.column(6).search('').draw(); // Status column
    } else {
        dataTable.column(6).search('^' + status + '$', true, false).draw();
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