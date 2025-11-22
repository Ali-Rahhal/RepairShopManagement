var dataTable;

$(function() {
    loadFilters();
    loadDataTable();
});

function isAdmin() {//function to check if the user is admin
    return document.getElementById("isAdmin").value === "True";
}

function loadFilters() {
    // Load models for filter
    $.ajax({
        url: '/Admin/SerialNumbers/Index?handler=Models',
        type: 'GET',
        success: function (data) {
            populateModelFilter(data.models);
        }
    });

    // Load clients for filter
    $.ajax({
        url: '/Admin/SerialNumbers/Index?handler=Clients',
        type: 'GET',
        success: function (data) {
            populateClientFilter(data.clients);
        }
    });
}

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "stateSave": true,
        "stateDuration": 86400, // Any positive number = sessionStorage (in seconds)
        // 86400 seconds = 24 hours, but sessionStorage lasts only for the browser session
        "ajax": {
            url: '/Admin/SerialNumbers/Index?handler=All'
        },
        "dom": '<"d-flex justify-content-between align-items-center mb-2"l<"ml-auto"f>>rtip',
        "order": [[6, "desc"]],
        "columns": [
            {
                data: 'value',
                "width": "15%",
                "render": function (data, type, row) {
                    return data || '-';
                }
            },
            {
                data: 'model.name',
                "width": "20%",
                "render": function (data, type, row) {
                    return data || 'N/A';
                }
            },
            {
                data: 'client.name',
                "width": "10%",
                "render": function (data, type, row) {
                    return data || 'N/A';
                }
            },
            {
                data: 'client.branch',
                "width": "10%",
                "render": function (data, type, row) {
                    return data || 'N/A';
                }
            },
            {
                data: 'warrantyId',
                "width": "15%",
                "render": function (data, type, row) {
                    return  (data !== null) ? `Warranty: #${data}`: 'No Warranty' ;
                }
            },
            {
                data: 'maintenanceContractId',
                "width": "15%",
                "render": function (data, type, row) {
                    return  (data !== null) ? `Contract: #${data}`: 'No Contract' ;
                }
            },
            {
                data: 'receivedDate',
                "width": "15%",
                "render": function (data, type, row) {
                    if (data) {
                        // Convert to Date object and format as dd-MM-yyyy HH:mm tt
                        const date = new Date(data);

                        // When DataTables needs to sort or order, return a numeric timestamp
                        if (type === 'sort' || type === 'order') {
                            return date.getTime();
                        }

                        return date.toLocaleDateString('en-GB')
                            .split('/').join('-') + ' ' +
                            date.toLocaleTimeString('en-US', {
                                hour: '2-digit',
                                minute: '2-digit',
                                hour12: true
                            });
                    }
                    return '';
                }
            },
            {
                data: 'id',
                "render": function (data) {
                    return `<div class="w-100 d-flex justify-content-center" role="group">
                        <a href="/Admin/SerialNumbers/Upsert?id=${data}" title="Edit" class="btn btn-primary mx-2"><i class="bi bi-pencil-square"></i></a>
                        <a onClick="Delete('/Admin/SerialNumbers/Index?handler=Delete&id=${data}')" title="Delete" class="btn btn-danger mx-2"><i class="bi bi-trash-fill"></i></a>
                    </div>`;
                },
                "width": "15%",
                "orderable": false
            }
        ],
        "language": {
            "emptyTable": "No serial numbers found",
            "zeroRecords": "No matching serial numbers found"
        }
    });

    // Add event listeners for filters
    $('#modelFilter').on('change', function () {
        applyFilters();
    });

    $('#clientFilter').on('change', function () {
        applyFilters();
    });
}

function populateModelFilter(models) {
    var select = $('#modelFilter');
    select.empty();
    select.append('<option value="All">All Models</option>');

    models.forEach(function (model) {
        select.append('<option value="' + model.name + '">' + model.name + '</option>');
    });
}

function populateClientFilter(clients) {
    var select = $('#clientFilter');
    select.empty();
    select.append('<option value="All">All Clients</option>');

    clients.forEach(function (client) {
        select.append('<option value="' + client.name + '">' + client.name + '</option>');
    });
}

function applyFilters() {
    var modelFilter = $('#modelFilter').val();
    var clientFilter = $('#clientFilter').val();

    // Clear all column searches first
    dataTable.columns().search('');

    // Apply model filter to column 1 (Model)
    if (modelFilter !== 'All') {
        dataTable.column(1).search('^' + modelFilter + '$', true, false);
    }

    // Apply client filter to column 2 (Client)
    if (clientFilter !== 'All') {
        dataTable.column(2).search('^' + clientFilter + '$', true, false);
    }

    dataTable.draw();
}

function clearFilters() {
    $('#modelFilter').val('All');
    $('#clientFilter').val('All');
    dataTable.order([
        [6, "desc"]
    ]).draw();
    dataTable.columns().search('').draw();
    toastr.info('All filters cleared');
}

function Delete(url) {
    Swal.fire({
        title: "Are you sure you want to delete this serial number?",
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
                        dataTable.ajax.reload(null, false);
                        // Reload filters to reflect changes
                        loadFilters();
                        toastr.success(data.message);
                    }
                    else {
                        toastr.error(data.message);
                    }
                },
                error: function () {
                    toastr.error('An error occurred while deleting the serial number');
                }
            });
        }
    });
}