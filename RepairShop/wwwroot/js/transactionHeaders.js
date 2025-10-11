//code needed for datatable to work in TH index page
var dataTable;

$(document).ready(function () {
    loadDataTable();
});

function isAdmin() {//function to check if the user is admin
    return document.getElementById("isAdmin").value === "True";
}

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "stateSave": true,
        "ajax": {
            url: '/User/TransactionHeaders/Index?handler=All',
            dataSrc: function (json) {
                // Extract unique clients for the filter
                var clients = [...new Set(json.data.map(item => item.client.name))];
                populateClientFilter(clients);

                return json.data;
            }
        },
        "dom": '<"d-flex justify-content-between align-items-center mb-2"l<"ml-auto"f>>rtip',
        "order": [
            [3, "asc"],   // Status: New -> InProgress -> Completed -> OutOfService
            [5, "desc"]   // Then by creation date: newest first
        ],
        "columnDefs": [
            {
                "targets": 3, // Status column
                "type": "status-priority"
            }
        ],
        "columns": [
            {
                data: 'user.userName',
                visible: isAdmin(),//this column is only visible to admin users
                "width": "10%"
            },
            {
                data: 'model',
                "width": "20%",
                render: function (data) {
                    return data || 'N/A';
                }
            },
            {
                data: 'serialNumber',
                "width": "15%",
                render: function (data) {
                    return data || 'N/A';
                }
            },
            {
                data: 'status',
                "render": function (data) {
                    switch (data) {
                        case "New":
                            return `<span class="badge bg-info p-2 fs-5">${data}</span>`;
                        case "InProgress":
                            return `<span class="badge bg-warning p-2 fs-5">${data}</span>`;
                        case "Completed":
                            return `<span class="badge bg-success p-2 fs-5">${data}</span>`;
                        case "OutOfService":
                            return `<span class="badge bg-danger p-2 fs-5">${data}</span>`;
                        default:
                            return `<p>${data}</p>`;
                    }
                },
                "width": "10%"
            },
            {
                data: 'client.name',
                "width": "15%",
                render: function (data) {
                    return data || 'N/A';
                }
            },
            {
                data: 'createdDate',
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
                visible: isAdmin(),//this column is only visible to non-admin users
                "render": function (data) {
                    return `<div class="w-75 d-flex" role="group">
                    <a href="/User/TransactionBodies/Index?HeaderId=${data}" title="View Parts" class="btn btn-info mx-2"><i class="bi bi-tools"></i></a>
                    <a onClick="Delete('/User/TransactionHeaders/Index?handler=Delete&id=${data}')" title="Delete" class="btn btn-danger mx-2"><i class="bi bi-trash-fill"></i></a>
                    </div>`;
                },
                "width": "5%"
            },
            {
                data: 'id',
                visible: !isAdmin(),//this column is only visible to non-admin users
                "render": function (data, type, row) {
                    //show status change button if status is "New" 
                    if (row.status === "New") {
                        return `<div class="w-75 d-flex" role="group">
                                    <a onClick="ChangeStatusToInProgress('/User/TransactionHeaders/Index?handler=ChangeStatus&id=${data}')" title="Start Work" class="btn btn-success mx-2"><i class="bi bi-play-circle"></i></a>
                                    <a href="/User/TransactionHeaders/Upsert?id=${data}" title="Edit" class="btn btn-primary mx-2"><i class="bi bi-pencil-square"></i></a>
                                    <a title="Complete(Transaction needs to be in progress)" class="btn btn-dark mx-2"><i class="bi bi-check-circle"></i></a>
                                </div>`;
                    } else if (row.status !== "Completed" && row.status !== "OutOfService") {
                        return `<div class="w-75 d-flex" role="group">
                                    <a href="/User/TransactionBodies/Index?HeaderId=${data}" title="View Parts" class="btn btn-info mx-2"><i class="bi bi-tools"></i></a> 
                                    <a href="/User/TransactionHeaders/Upsert?id=${data}" title="Edit" class="btn btn-primary mx-2"><i class="bi bi-pencil-square"></i></a>
                                    <a onClick="ToCompleted('/User/TransactionHeaders/Index?handler=CompleteStatus&id=${data}')" title="Complete" class="btn btn-success mx-2"><i class="bi bi-check-circle"></i></a>
                                </div>`;
                    } else {
                        return `<div class="w-75 d-flex" role="group">
                                    <a href="/User/TransactionBodies/Index?HeaderId=${data}" title="View Parts" class="btn btn-info mx-2"><i class="bi bi-tools"></i></a> 
                                    <a title="Edit(Transaction is completed)" class="btn btn-dark mx-2"><i class="bi bi-pencil-square"></i></a>
                                    <a title="Complete(Transaction is completed)" class="btn btn-dark mx-2"><i class="bi bi-check-circle"></i></a>
                                </div>`;
                    }
                },
                "width": "20%"
            }
        ],
        "language": {
            "emptyTable": "No transactions found",
            "zeroRecords": "No matching transactions found"
        }
    });

    // Add event listeners for filters
    $('#clientFilter').on('change', function () {
        applyFilters();
    });

    $('#statusFilter').on('change', function () {
        applyFilters();
    });

    $('#minDate').on('change', function () {
        applyFilters();
    });

    $('#maxDate').on('change', function () {
        applyFilters();
    });

    // robust status-priority sorter (register before DataTable init) -----
    $.fn.dataTable.ext.type.order['status-priority-pre'] = function (data) {
        // handle null/undefined
        if (data == null) return 99;

        // if cell contains HTML (badge), strip tags -> get inner text
        if (typeof data === 'string') {
            // remove tags, unescape &nbsp; etc, trim whitespace
            data = data.replace(/<[^>]*>/g, '').replace(/\u00A0/g, ' ').trim();
        } else {
            data = String(data);
        }

        // normalize: remove spaces and lowercase for robust comparison
        var key = data.replace(/\s+/g, '').toLowerCase();

        switch (key) {
            case 'new': return 1;
            case 'inprogress': return 2;
            case 'completed': return 3;
            case 'outofservice': return 4;
            default: return 5;
        }
    };

    // Custom filtering function for date + client + status
    $.fn.dataTable.ext.search.push(
        function (settings, data, dataIndex) {
            var min = $('#minDate').val();
            var max = $('#maxDate').val();
            var clientFilter = $('#clientFilter').val();
            var statusFilter = $('#statusFilter').val();

            var rowData = dataTable.row(dataIndex).data();
            var createdDate = new Date(rowData.createdDate);
            var clientName = rowData.client.name;
            var status = rowData.status;

            // Date filter
            if (min) {
                var minDate = new Date(min);
                minDate.setHours(0, 0, 0, 0); // Start of day
                if (createdDate < minDate) return false;
            }
            if (max) {
                var maxDate = new Date(max);
                maxDate.setHours(23, 59, 59, 999); // End of day
                if (createdDate > maxDate) return false;
            }

            // Client filter
            if (clientFilter !== 'All') {
                if (clientName !== clientFilter) return false;
            }

            // Status filter
            if (statusFilter !== 'All') {
                if (status !== statusFilter) return false;
            }

            return true;
        }
    );
}

function populateClientFilter(clients) {
    var select = $('#clientFilter');
    select.empty();
    select.append('<option value="All">All Clients</option>');

    // Sort clients alphabetically
    clients.sort().forEach(function (client) {
        select.append('<option value="' + client + '">' + client + '</option>');
    });
}

function applyFilters() {
    // The filtering is handled by the custom search function
    dataTable.draw();
}

function clearFilters() {
    $('#clientFilter').val('All');
    $('#statusFilter').val('All');
    $('#minDate').val('');
    $('#maxDate').val('');
    dataTable.columns().search('').draw();

    if (typeof toastr !== 'undefined') {
        toastr.info('All filters cleared');
    }
}

// Function to change status from "New" to "InProgress"
function ChangeStatusToInProgress(url) {
    $.ajax({
        url: url,
        success: function (data) {
            if (data.success) {
                dataTable.ajax.reload(); // Reload to reflect the status change
                toastr.success(data.message);
            } else {
                toastr.error(data.message);
            }
        }
    });
}

//function for sweet alert delete confirmation
function Delete(url) {
    Swal.fire({
        title: "Are you sure you want to delete this transaction?",
        text: "You won't be able to revert this!",
        icon: "warning",
        showCancelButton: true,
        confirmButtonColor: "#3085d6",
        cancelButtonColor: "#d33",
        confirmButtonText: "Yes, delete it!"
    }).then((result) => {
        if (result.isConfirmed) {//if user clicks on yes, delete it button
            $.ajax({
                url: url,//url is passed from the Delete function call in the datatable render method.
                success: function (data) {//data is the json returned from the OnGetDelete method in the page model.
                    if (data.success) {
                        dataTable.ajax.reload();//reload the datatable to reflect the changes.
                        toastr.success(data.message);//show success message using toastr.
                    }
                    else {
                        toastr.error(data.message);//show error message using toastr.
                    }
                }
            })
        }
    });
}

function ToCompleted(url) {
    Swal.fire({
        title: "Are you sure you want to mark this task as complete?",
        text: "You won't be able to revert this!",
        icon: "question",
        showCancelButton: true,
        confirmButtonColor: "#3085d6",
        cancelButtonColor: "#d33",
        confirmButtonText: "Yes, mark as complete"
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: url,
                success: function (data) {
                    if (data.success) {
                        dataTable.ajax.reload();
                        toastr.success(data.message);
                    }
                    else {
                        toastr.error(data.message);
                    }
                }
            })
        }
    });
}