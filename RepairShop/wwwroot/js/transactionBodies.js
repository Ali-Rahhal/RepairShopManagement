//code needed for datatable to work in TH index page
var dataTable;
let hId = document.getElementById("HeaderId").value;
let hStatus = document.getElementById("HeaderStatus").value;

$(document).ready(function () {
    loadDataTable();
});

function isAdmin() {//function to check if the user is admin
    return document.getElementById("isAdmin").value === "True";
}

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "stateSave": true,
        "ajax": { url: `/User/TransactionBodies/Index?handler=All&headerId=${hId}` },
        "dom": '<"d-flex justify-content-between align-items-center mb-2"l<"ml-auto"f>>rtip',
        "columns": [
            { data: 'brokenPartName', "width": "20%" },//dont forget the names should match the property names.
            {
                data: 'status',
                "render": function (data) {
                    switch (data) {
                        case "PendingForRepair":
                            return `<span class="badge bg-info p-2 fs-5">${data}</span>`;
                        case "PendingForReplacement":
                            return `<span class="badge bg-warning p-2 fs-5">${data}</span>`;
                        case "WaitingForPart":
                            return `<span class="badge bg-primary p-2 fs-5">${data}</span>`;
                        case "Fixed":
                            return `<span class="badge bg-success p-2 fs-5">${data}</span>`;
                        case "Replaced":
                            return `<span class="badge bg-success p-2 fs-5">${data}</span>`;
                        case "NotRepairable":
                            return `<span class="badge bg-danger p-2 fs-5">${data}</span>`;
                        case "NotReplaceable":
                            return `<span class="badge bg-danger p-2 fs-5">${data}</span>`;
                        default:
                            return `<p>${data}</p>`;
                    }
                },
                "width": "20%"
            },
            { data: 'partName', "width": "20%" },//dont forget the names should match the property names.
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
                data: 'id',//this column is only visible to non-admin users
                "render": function (data, type, row) {
                    console.log(isAdmin());

                    return `<div class="w-100 d-flex justify-content-center" role="group">
                                    <button class="btn btn-outline-primary mx-2" title="View History"
                                        onclick="showTBHistory('${row.brokenPartName}', '${row.createdDate}', 
                                                                '${row.waitingPartDate}', '${row.fixedDate}', '${row.replacedDate}', 
                                                                '${row.notRepairableDate}', '${row.notReplaceableDate}')">
                                            <i class="bi bi-clock-history"></i>
                                    </button>
                                </div>`;
                },
                "width": "5%"
            },
            {
                data: 'id',//this column is only visible to non-admin users
                "render": function (data, type, row) {
                    console.log(isAdmin());
                    if (hStatus === "Completed" || hStatus === "OutOfService") {//only show this column if the header status is completed.
                        return `<div class="w-100 d-flex justify-content-center" role="group">
                                    <button class="btn btn-outline-primary mx-2" title="View History"
                                        onclick="showTBHistory('${row.brokenPartName}', '${row.createdDate}', 
                                                                '${row.waitingPartDate}', '${row.fixedDate}', '${row.replacedDate}', 
                                                                '${row.notRepairableDate}', '${row.notReplaceableDate}')">
                                            <i class="bi bi-clock-history"></i>
                                    </button>
                                </div>`;
                    }
                    if (row.status === "WaitingForPart") {//only show this column if the part status is waiting for part.
                        return `<div class="w-100 d-flex justify-content-center" role="group">
                                    <a onClick="CheckPartAvailable('/User/TransactionBodies/Index?handler=CheckPart&id=${data}')" title="Check if Part is Available" class="btn btn-info mx-2"><i class="bi bi-box-seam"></i></a>
                                    <a href="/User/TransactionBodies/Upsert?id=${data}" title="Edit Name" class="btn btn-primary mx-2"><i class="bi bi-pencil-square"></i></a>
                                    <a onClick="Delete('/User/TransactionBodies/Index?handler=Delete&id=${data}')" title="Delete" class="btn btn-danger mx-2"><i class="bi bi-trash-fill"></i></a>
                                    <button class="btn btn-outline-primary mx-2" title="View History"
                                        onclick="showTBHistory('${row.brokenPartName}', '${row.createdDate}', 
                                                                '${row.waitingPartDate}', '${row.fixedDate}', '${row.replacedDate}', 
                                                                '${row.notRepairableDate}', '${row.notReplaceableDate}')">
                                            <i class="bi bi-clock-history"></i>
                                    </button>
                                </div>`;
                    } else {
                        return `<div class="w-100 d-flex justify-content-center" role="group">
                                    <a href="/User/TransactionBodies/Upsert?id=${data}" title="Edit Name" class="btn btn-primary mx-2"><i class="bi bi-pencil-square"></i></a>
                                    <a onClick="Delete('/User/TransactionBodies/Index?handler=Delete&id=${data}')" title="Delete" class="btn btn-danger mx-2"><i class="bi bi-trash-fill"></i></a>
                                    <a onClick="ChangeStatus(${data}, '${row.status}')" title="Change Status" class="btn btn-warning mx-2"><i class="bi bi-gear"></i></a>
                                    <button class="btn btn-outline-primary mx-2" title="View History"
                                        onclick="showTBHistory('${row.brokenPartName}', '${row.createdDate}', 
                                                                '${row.waitingPartDate}', '${row.fixedDate}', '${row.replacedDate}', 
                                                                '${row.notRepairableDate}', '${row.notReplaceableDate}')">
                                            <i class="bi bi-clock-history"></i>
                                    </button>
                                </div>`;
                    } 
                },
                "width": "20%"
            }
        ]
    });

    if (isAdmin()) {
        dataTable.column(4).visible(true);   // show admin column
        dataTable.column(5).visible(false);  // hide non-admin column
    } else {
        dataTable.column(4).visible(false);
        dataTable.column(5).visible(true);
    }
}

// Function to check if the part is available and update the status
function CheckPartAvailable(url) {
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


function Delete(url) {
    Swal.fire({
        title: "Are you sure you want to delete this part?",
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

// Function to change the status
function ChangeStatus(id, currentStatus) {
    let confirmButtonText, denyButtonText;
    let confirmUrl, denyUrl;

    if (currentStatus === 'PendingForRepair') {
        confirmButtonText = 'Fixed';
        denyButtonText = 'Not Repairable';
        confirmUrl = `/User/TransactionBodies/Index?handler=Status&id=${id}&choice=1`; // Fixed
        denyUrl = `/User/TransactionBodies/Index?handler=Status&id=${id}&choice=0`; // Not Repairable
    } else if (currentStatus === 'PendingForReplacement') {
        confirmButtonText = 'Replaced';
        denyButtonText = 'Not Replaceable';
        confirmUrl = `/User/TransactionBodies/Index?handler=Status&id=${id}&choice=3`; // Replaced
        denyUrl = `/User/TransactionBodies/Index?handler=Status&id=${id}&choice=2`; // Not Replaceable
    } else {
        // If status is already final, show appropriate message
        toastr.info('This part already has a final status and cannot be changed.');
        return;
    }

    Swal.fire({
        title: 'Choose Action',
        text: 'What is the status of your part?',
        icon: 'question',

        // --- Button Configuration ---
        showConfirmButton: true, // Shows the "Confirm" button
        showDenyButton: true,   // Shows the "Deny" button
        showCancelButton: true, // Shows the "Cancel" button

        // Text for the buttons (dynamic based on status)
        confirmButtonText: confirmButtonText,
        denyButtonText: denyButtonText,
        cancelButtonText: 'Close',

        // Button colors (optional, for better visual distinction)
        confirmButtonColor: '#198754', // Green for success actions
        denyButtonColor: '#dc3545',    // Red for negative actions
        cancelButtonColor: '#6c757d'   // Gray for cancel

    }).then((result) => {
        if (result.isConfirmed) {
            // CONFIRM Button clicked (Fixed or Replaced)
            $.ajax({
                url: confirmUrl,
                success: function (data) {
                    if (data.success) {
                        dataTable.ajax.reload();
                        toastr.success(data.message);
                    } else {
                        toastr.error(data.message);
                    }
                },
                error: function () {
                    toastr.error('An error occurred while updating status.');
                }
            })
        } else if (result.isDenied) {
            // DENY Button clicked (Not Repairable or Not Replaceable)
            $.ajax({
                url: denyUrl,
                success: function (data) {
                    if (data.success) {
                        dataTable.ajax.reload();
                        toastr.success(data.message);
                    } else {
                        toastr.error(data.message);
                    }
                },
                error: function () {
                    toastr.error('An error occurred while updating status.');
                }
            })
        }
        // If result.isDismissed (Cancel or background click), nothing happens.
    });
}

function showTBHistory(partName, created, waitingPart, fixed, replaced, notRepairable, notReplaceable) {
    const formatDate = (d) => {
        const date = new Date(d);
        if (isNaN(date)) return '-';

        return date.toLocaleDateString('en-GB')
            .split('/').join('-') + ' ' +
            date.toLocaleTimeString('en-US', {
                hour: '2-digit',
                minute: '2-digit',
                hour12: true
            });
    };

    let html = `
    <div style="text-align:left;">
        <p><strong>Broken Part:</strong> ${partName}</p>
        <hr>
        ${formatDate(created) !== '-'
            ? `<p><strong>Created:</strong> ${formatDate(created)}</p>`
            : ''
        }
        ${formatDate(waitingPart) !== '-'
            ? `<p><strong>Waiting for Part:</strong> ${formatDate(waitingPart)}</p>`
            : ''
        }
        ${formatDate(fixed) !== '-'
            ? `<p><strong>Fixed:</strong> ${formatDate(fixed)}</p>`
            : ''
        }
        ${formatDate(replaced) !== '-'
            ? `<p><strong>Replaced:</strong> ${formatDate(replaced)}</p>`
            : ''
        }
        ${formatDate(notRepairable) !== '-'
            ? `<p><strong>Not Repairable:</strong> ${formatDate(notRepairable)}</p>`
            : ''
        }
        ${formatDate(notReplaceable) !== '-'
            ? `<p><strong>Not Replaceable:</strong> ${formatDate(notReplaceable)}</p>`
            : ''
        }
    </div>`

    Swal.fire({
        title: `<i class="bi bi-clock-history"></i> Part History`,
        html: html,
        icon: 'info',
        confirmButtonText: 'Close',
        customClass: { popup: 'swal-wide' }
    });
}