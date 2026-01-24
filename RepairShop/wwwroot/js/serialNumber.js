var dataTable;

function loadFilters() {
    // Load models
    $.get('/Admin/SerialNumbers/Index?handler=Models', function (data) {
        populateModelFilter(data.models);
    });

    // Load clients
    $.get('/Admin/SerialNumbers/Index?handler=Clients', function (data) {
        populateClientFilter(data.clients);
    });
}

$(function () {
    loadFilters();
    loadDataTable();
});

function isAdmin() {
    try {
        return document.getElementById("isAdmin").value === "True";
    } catch {
        return false;
    }
}

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        serverSide: true,
        processing: true,
        paging: true,
        pageLength: 10,
        lengthMenu: [5, 10, 25, 50, 100],
        searchDelay: 500,
        stateSave: true,
        stateDuration: 86400,
        order: [], // default
        ajax: {
            url: '/Admin/SerialNumbers/Index?handler=All',
            type: 'GET',
            data: function (d) {
                // Add custom filters to request
                d.modelId = $('#modelFilter').val() || '';
                d.clientId = $('#clientFilter').val() || '';
                return d;
            },
            error: function (xhr, error, thrown) {
                console.error('DataTable Ajax Error:', error, thrown);
                toastr.error('Failed to load serial numbers. Please try again.');
            }
        },
        "dom": '<"d-flex justify-content-between align-items-center mb-2"l<"ml-auto"f>>rtip',

        columns: [
            {
                data: 'value',
                width: "15%",
                className: "text-start",
                render: function (data) {
                    return data || '-';
                }
            },
            {
                data: 'modelName',
                width: "15%",
                className: "text-start",
                render: function (data) {
                    return data || '-';
                }
            },
            {
                data: 'clientName',
                width: "15%",
                className: "text-start",
                render: function (data) {
                    return data || '-';
                }
            },
            {
                data: 'branchName',
                width: "10%",
                className: "text-start",
                render: function (data) {
                    return data === 'N/A' ? '-' : data;
                }
            },
            {
                data: 'maintenanceContractId',
                width: "10%",
                orderable: false,
                className: "text-center",
                render: function (data) {
                    return data ? `<span class="badge bg-info">${data}</span>` : '-';
                }
            },
            {
                data: 'warrantyId',
                width: "10%",
                orderable: false,
                className: "text-center",
                render: function (data) {
                    return data ? `<span class="badge bg-warning">${data}</span>` : '-';
                }
            },
            {
                data: 'receivedDate',
                width: "10%",
                className: "text-center",
                render: function (data) {
                    return data || '-';
                }
            },
            {
                data: 'id',
                orderable: false,
                searchable: false,
                width: "15%",
                className: "text-center",
                "render": function (data) {
                    return `<div class="w-100 d-flex justify-content-center" role="group">
                        <a href="/Admin/SerialNumbers/Upsert?id=${data}" title="Edit" class="btn btn-primary mx-2"><i class="bi bi-pencil-square"></i></a>
                        <a onClick="Delete(${data})" title="Delete" class="btn btn-danger mx-2"><i class="bi bi-trash-fill"></i></a>
                    </div>`;
                },
            }
        ],

        language: {
            emptyTable: "No serial numbers found",
            zeroRecords: "No matching serial numbers found"
        }
    });

    // Clear search when filters change
    $('#modelFilter, #clientFilter').on('change', function () {
        $('#tblData_filter input').val('');
        dataTable.search('').draw();
    });
}

// ================= FILTER FUNCTIONS =================

function populateModelFilter(models) {
    var select = $('#modelFilter');
    select.empty();
    select.append('<option value="">All Models</option>');

    models.forEach(function (model) {
        select.append(`<option value="${model.id}">${model.name}</option>`);
    });
}

function populateClientFilter(clients) {
    var select = $('#clientFilter');
    select.empty();
    select.append('<option value="">All Clients</option>');

    clients.forEach(function (client) {
        select.append(`<option value="${client.id}">${client.name}</option>`);
    });
}

function clearFilters() {
    $('#modelFilter').val('');
    $('#clientFilter').val('');
    dataTable.order([]).ajax.reload(null, false); // Reset ordering to ReceivedDate desc
    toastr.info('All filters and sorting reset');
}

// ================= DELETE FUNCTION =================

function Delete(id) {
    Swal.fire({
        title: 'Delete Serial Number?',
        html: `
            <div class="mb-3">
                <p class="text-danger">This action cannot be undone!</p>
                <label for="deleteReason" class="form-label">Reason for deletion:</label>
                <input type="text" id="deleteReason" class="form-control" 
                       placeholder="Enter reason (required)" required>
            </div>
        `,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#3085d6',
        confirmButtonText: 'Delete',
        cancelButtonText: 'Cancel',
        reverseButtons: true,
        preConfirm: () => {
            const reason = $('#deleteReason').val();
            if (!reason || reason.trim() === '') {
                Swal.showValidationMessage('Please enter a reason for deletion');
                return false;
            }
            return reason.trim();
        }
    }).then((result) => {
        if (result.isConfirmed) {
            const reason = result.value;

            $.ajax({
                url: `/Admin/SerialNumbers/Index?handler=Delete&id=${id}`,
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(reason),
                headers: {
                    'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
                },
                beforeSend: function () {
                    Swal.showLoading();
                },
                success: function (data) {
                    Swal.close();
                    if (data.success) {
                        dataTable.ajax.reload(null, false);
                        toastr.success(data.message);
                    } else {
                        toastr.error(data.message);
                    }
                },
                error: function () {
                    Swal.close();
                    toastr.error('An error occurred while deleting the serial number');
                }
            });
        }
    });
}