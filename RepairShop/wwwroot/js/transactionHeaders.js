//code needed for datatable to work in TH index page
var dataTable;

$(document).ready(function () {
    sessionStorage.clear();
    loadDataTable();
});

function isAdmin() {//function to check if the user is admin
    return document.getElementById("isAdmin").value === "True";
}

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "stateSave": true,
        "stateDuration": 86400, // Any positive number = sessionStorage (in seconds)
        // 86400 seconds = 24 hours, but sessionStorage lasts only for the browser session
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
            [9, "desc"],  // Most recently edited transactions (based on LastModifiedDate) come first
            [4, "asc"],   // Within equal modification times, sort by status
            [7, "desc"]   // Within equal statuses, sort by newest creation date
        ],
        "columnDefs": [
            {
                "targets": 4, // Status column
                "type": "status-priority"
            }
        ],
        "columns": [
            {
                data: 'user.userName',
                "width": "5%",
                visible: isAdmin()//this column is only visible to admin users
            },
            {
                data: 'model',
                "width": "10%",
                render: function (data) {
                    return data || 'N/A';
                }
            },
            {
                data: 'serialNumber',
                "width": "10%",
                render: function (data) {
                    return data || 'N/A';
                }
            },
            {
                data: 'duDescription',
                "width": "15%",
                render: function (data, type, row) {
                    var text = '';
                    if (data) {
                        text = data.length > 20 ? data.substring(0, 20) + '...' : data;
                        // Add view full description button if text is truncated
                        if (data.length > 20) {
                            const safeDescription = data.replace(/"/g, '&quot;').replace(/'/g, '&#39;');
                            return `
                                ${text} 
                                <button class="btn btn-sm btn-outline-info ms-1 p-0" 
                                        style="width: 20px; height: 20px; font-size: 10px;"
                                        onclick="showFullDescription('${safeDescription}')"
                                        title="View full description">
                                    <i class="bi bi-eye"></i>
                                </button>
                            `;
                        }
                    }
                    return text || 'N/A';
                }
            },
            {
                data: 'status',
                "width": "5%",
                "render": function (data) {
                    switch (data) {
                        case "New":
                            return `<span class="badge bg-info">${data}</span>`;
                        case "InProgress":
                            return `<span class="badge bg-warning">${data}</span>`;
                        case "Completed":
                            return `<span class="badge bg-success">${data}</span>`;
                        case "OutOfService":
                            return `<span class="badge bg-danger">${data}</span>`;
                        case "Delivered":
                            return `<span class="badge bg-primary">${data}</span>`;
                        default:
                            return `<p>${data}</p>`;
                    }
                }
            },
            {
                data: 'client.name',
                "width": "10%",
                render: function (data) {
                    return data || 'N/A';
                }
            },
            {
                data: 'client.branch',
                "width": "10%",
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
                "width": "20%",
                "render": function (data, type, row) {
                    let firstButton = row.status === "New"
                        ? `<a onClick="ChangeStatusToInProgress('/User/TransactionHeaders/Index?handler=ChangeStatus&id=${data}')" title="Start Work" class="btn btn-success mx-2"><i class="bi bi-play-circle"></i></a>`
                        : `<a href="/User/TransactionBodies/Index?HeaderId=${data}" title="View Parts" class="btn btn-info mx-2"><i class="bi bi-tools"></i></a>`;

                    let secondButton = (row.status === "New" || row.status === "InProgress")
                        ? `<a href="/User/TransactionHeaders/Upsert?id=${data}" title="Edit" class="btn btn-primary mx-2"><i class="bi bi-pencil-square"></i></a>`
                        : `<a title="Edit(Transaction is completed)" class="btn btn-dark mx-2"><i class="bi bi-pencil-square"></i></a>`;

                    let thirdButton = ``;
                    if (row.status === "New") {
                        thirdButton = `<a title="Complete(Transaction needs to be in progress)" class="btn btn-dark mx-2"><i class="bi bi-check-circle"></i></a>`;
                    }
                    else if (row.status === "InProgress") {
                        thirdButton = `<a onClick="ToBeCompleted('/User/TransactionHeaders/Index?handler=CompleteStatus&id=${data}')" title="Complete" class="btn btn-success mx-2"><i class="bi bi-check-circle"></i></a>`;
                    }
                    else if (row.status === "Completed") {
                        thirdButton = `<a onClick="ToBeDelivered('/User/TransactionHeaders/Index?handler=DeliverStatus&id=${data}')" title="Deliver" class="btn btn-outline-success mx-2"><i class="bi bi-truck"></i></a>`;
                    }
                    else if (row.status === "OutOfService") {
                        thirdButton = `<a title="Deliver(Transaction is out of service)" class="btn btn-dark mx-2"><i class="bi bi-truck"></i></a>`;
                    }
                    else if (row.status === "Delivered") {
                        thirdButton = `<a href="/User/TransactionHeaders/Index?handler=DownloadPdf&id=${data}" title="Download PDF Report" class="btn btn-info mx-2" target="_blank"> <i class="bi bi-file-earmark-pdf"></i></a>`;
                    }

                    let fourthButton = `<button onclick="showHistory('${row.id}', '${row.createdDate}', '${row.inProgressDate}', '${row.completedOrOutOfServiceDate}', '${row.deliveredDate}')"
                                        title="View History" class="btn btn-outline-primary mx-2"> 
                                            <i class="bi bi-clock-history"></i>
                                        </button>`;

                    let fifthButton = isAdmin()
                        ? `<a onClick="Delete('/User/TransactionHeaders/Index?handler=Delete&id=${data}')" title="Delete" class="btn btn-danger mx-2"><i class="bi bi-trash-fill"></i></a>`
                        : ``;

                    return `<div class="w-75 d-flex" role="group">
                                ${firstButton}
                                ${secondButton}
                                ${thirdButton}
                                ${fourthButton}
                                ${fifthButton}
                            </div>`;
                }
            },
            {
                data: 'lastModifiedDate',
                visible: false // hide from user, used for ordering
            }
        ],
        "language": {
            "emptyTable": "No transactions found",
            "zeroRecords": "No matching transactions found"
        }
    });

    // Add event listeners for filters
    $('#clientFilter').on('change', function () {
        dataTable.draw();
    });

    $('#statusFilter').on('change', function () {
        dataTable.draw();
    });

    $('#minDate').on('change', function () {
        dataTable.draw();
    });

    $('#maxDate').on('change', function () {
        dataTable.draw();
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
            case 'delivered': return 4;
            case 'outofservice': return 5;
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

function clearFilters() {
    $('#clientFilter').val('All');
    $('#statusFilter').val('All');
    $('#minDate').val('');
    $('#maxDate').val('');
    dataTable.order([
        [9, "desc"],  // Most recently edited transactions (based on LastModifiedDate) come first
        [4, "asc"],   // Within equal modification times, sort by status
        [7, "desc"]   // Within equal statuses, sort by newest creation date
    ]).draw();

    // Clear any search filters
    dataTable.search('').columns().search('').draw();

    if (typeof toastr !== 'undefined') {
        toastr.info('All filters and sorting reset');
    }
}

function showFullDescription(description) {
    // Create a temporary div to escape HTML properly
    const tempDiv = document.createElement('div');
    tempDiv.textContent = description;
    const escapedDescription = tempDiv.innerHTML;

    Swal.fire({
        title: '<i class="bi bi-card-text"></i> Full Description',
        html: `
            <div style="text-align: left; max-height: 400px; overflow-y: auto; background: #f8f9fa; padding: 15px; border-radius: 5px; border: 1px solid #dee2e6;">
                <p style="margin: 0; white-space: pre-wrap; word-wrap: break-word;">${escapedDescription}</p>
            </div>
        `,
        icon: 'info',
        confirmButtonText: 'Close',
        width: '600px',
        customClass: {
            popup: 'swal-wide'
        }
    });
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
        html: "<p style='color: #dc3545; font-weight: 500;'>This will delete all the broken parts and the defective unit associated with this transaction!</p>",
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

function ToBeDelivered(url) {
    Swal.fire({
        title: "Are you sure you want to mark this task as delivered?",
        text: "You won't be able to revert this!",
        icon: "question",
        showCancelButton: true,
        confirmButtonColor: "#3085d6",
        cancelButtonColor: "#d33",
        confirmButtonText: "Yes, mark as delivered"
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

function showHistory(id, created, inProgress, completedOrOutOfService, delivered) {
    // Format dates nicely
    const formatDate = (d) => {
        const date = new Date(d);
        if(isNaN(date)) return '-';

        return date.toLocaleDateString('en-GB')
            .split('/').join('-') + ' ' +
            date.toLocaleTimeString('en-US', {
                hour: '2-digit',
                minute: '2-digit',
                hour12: true
            });
    };

    const html = `
        <div style="text-align:left;">
            <p><strong>Created:</strong> ${formatDate(created)}</p>
            <p><strong>In Progress:</strong> ${formatDate(inProgress)}</p>
            <p><strong>Completed / Out of Service:</strong> ${formatDate(completedOrOutOfService)}</p>
            <p><strong>Delivered:</strong> ${formatDate(delivered)}</p>
        </div>
    `;

    Swal.fire({
        title: `<i class="bi bi-clock-history"></i> Transaction #${id} History`,
        html: html,
        icon: 'info',
        confirmButtonText: 'Close',
        customClass: {
            popup: 'swal-wide'
        }
    });
}

function ToBeCompleted(url) {
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
                        const params = new URLSearchParams(url.split("?")[1]);
                        const headerId = params.get("id");
                        if (data.message.includes("Parts must be reported before marking as completed")) {
                            window.location.href = `/User/TransactionBodies/Upsert?headerId=${headerId}&fromCompletionBtn=true`;
                        } else if (data.message.includes("You have pending parts")) {
                            window.location.href = `/User/TransactionBodies/Index?headerId=${headerId}&fromCompletionBtn=true`;
                        }
                        toastr.error(data.message);
                    }
                }
            })
        }
    });
}