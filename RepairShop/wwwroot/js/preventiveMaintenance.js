var dataTable;
var selectedClientId = 0;
var selectedClientName = "";

$(document).ready(function () {
    // Initialize TomSelect
    new TomSelect("#clientFilter", {
        placeholder: "Select a Client...",
        allowEmptyOption: true,
        maxItems: 1,
        searchField: ["text"],
        sortField: {
            field: "text",
            direction: "asc"
        }
    });
    // Initialize the client filter
    initializeClientFilter();

    // ⭐ RESTORE SELECTED CLIENT FROM localStorage
    let savedClientId = localStorage.getItem("selectedClientId");

    if (savedClientId && savedClientId !== "0") {
        selectedClientId = savedClientId;

        // Set it in TomSelect
        $('#clientFilter')[0].tomselect.setValue(savedClientId);

        // Auto-load records
        loadRecordsForSelectedClient();
    }

    // Don't load DataTable on page load anymore
    // loadDataTable(); // Remove this line

    // Set up event handlers
    $('#btnLoadRecords').on('click', function () {
        loadRecordsForSelectedClient();
    });

    $('#btnClearFilter').on('click', function () {
        clearFilter();
    });

    // Allow Enter key to trigger filter
    $('#clientFilter').on('keypress', function (e) {
        if (e.which === 13) { // Enter key
            loadRecordsForSelectedClient();
        }
    });

    // ⭐ AUTO-LOAD WHEN GET CLIENT ID IS PASSED
    var preselectedClientId = $('#clientFilter')[0].tomselect.getValue();
    if (preselectedClientId && preselectedClientId !== "0") {
        selectedClientId = preselectedClientId;
        loadRecordsForSelectedClient();
    }
});

function initializeClientFilter() {
    var select = $('#clientFilter')[0].tomselect;
    var clientOptions = {};

    for (const key in select.options) {
        if (key !== "0") {
            clientOptions[key] = select.options[key].text;
        }
    }

    $('#clientFilter').data('clientNames', clientOptions);
}

function isAdmin() {
    return document.getElementById("isAdmin").value === "True";
}

function loadRecordsForSelectedClient() {
    selectedClientId = $('#clientFilter')[0].tomselect.getValue();

    if (selectedClientId === "0" || selectedClientId === "") {
        Swal.fire({
            title: "Please Select a Client",
            text: "You need to select a client first.",
            icon: "warning",
            confirmButtonColor: "#3085d6",
            confirmButtonText: "OK"
        });
        return;
    }

    // Get client name for display
    var clientNames = $('#clientFilter').data('clientNames');
    selectedClientName = clientNames[selectedClientId] || "Selected Client";

    // Update display
    $('#clientNameDisplay').html(`<i class="bi bi-building me-2"></i>${selectedClientName} - Preventive Maintenance Records`);

    // Show records section, hide empty state
    $('#recordsSection').removeClass('d-none');
    $('#emptyState').addClass('d-none');

    // Update Add New Record button route
    $('#btnAddNewRecord').attr('href', `/User/PreventiveMaintenanceRecords/Upsert?clientId=${selectedClientId}`);

    localStorage.setItem("selectedClientId", selectedClientId);

    // Initialize or reload DataTable
    if (typeof dataTable === 'undefined') {
        loadDataTable();
    } else {
        dataTable.ajax.reload();
    }
}

function clearFilter() {
    localStorage.removeItem("selectedClientId");
    // Reset dropdown
    $('#clientFilter')[0].tomselect.clear();
    selectedClientId = 0;
    selectedClientName = "";

    // Hide records section, show empty state
    $('#recordsSection').addClass('d-none');
    $('#emptyState').removeClass('d-none');

    // Clear the table if it exists
    if (typeof dataTable !== 'undefined') {
        dataTable.clear().draw();
    }
}

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "stateSave": true,
        stateLoadParams: function (settings, data) {
            delete data.order; // remove saved order only
        },
        "stateDuration": 86400,
        "ajax": {
            url: '/User/PreventiveMaintenanceRecords/Index?handler=All',
            data: function (d) {
                // Add clientId to the request
                return $.extend({}, d, {
                    clientId: selectedClientId
                });
            }
        },
        "dom": '<"d-flex justify-content-between align-items-center mb-2"l<"ml-auto"f>>rtip',
        "order": [[10, "desc"]],
        "columns": [
            {
                data: 'departmentLocation',
                "width": "5%",
                "render": function (data) {
                    return data || 'N/A';
                }
            },
            {
                data: 'serialNumber.model.name',
                "width": "15%",
                "render": function (data) {
                    return data || 'N/A';
                }
            },
            {
                data: 'serialNumber.value',
                "width": "10%",
                "render": function (data) {
                    return data || 'N/A';
                }
            },
            {
                data: 'ipAddress',
                "width": "10%",
                "render": function (data) {
                    return data || 'N/A';
                }
            },
            {
                data: 'purchaseDate',
                "width": "5%",
                "render": function (data) {
                    if (!data) return 'N/A';
                    var date = new Date(data);
                    return date.toLocaleDateString();
                }
            },
            {
                data: 'problem',
                "width": "15%",
                "render": function (data) {
                    let text = '';
                    if (data) {
                        // Remove newlines and extra spaces for the truncated display
                        const cleanData = data.replace(/\s+/g, ' ').trim();
                        text = cleanData.length > 15 ? cleanData.substring(0, 15) + '...' : cleanData;

                        // Add view full description button if text is truncated
                        if (cleanData.length > 15) {
                            // Properly escape the string for HTML attribute including newlines
                            const safeDescription = data
                                .replace(/"/g, '&quot;')
                                .replace(/'/g, '&#39;')
                                .replace(/\r?\n/g, '\\n') // Escape newlines
                                .replace(/\t/g, '\\t');   // Escape tabs

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
                data: 'solution',
                "width": "15%",
                "render": function (data) {
                    let text = '';
                    if (data) {
                        // Remove newlines and extra spaces for the truncated display
                        const cleanData = data.replace(/\s+/g, ' ').trim();
                        text = cleanData.length > 15 ? cleanData.substring(0, 15) + '...' : cleanData;

                        // Add view full description button if text is truncated
                        if (cleanData.length > 15) {
                            // Properly escape the string for HTML attribute including newlines
                            const safeDescription = data
                                .replace(/"/g, '&quot;')
                                .replace(/'/g, '&#39;')
                                .replace(/\r?\n/g, '\\n') // Escape newlines
                                .replace(/\t/g, '\\t');   // Escape tabs

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
                data: 'checkupDate',
                "width": "5%",
                "render": function (data) {
                    if (!data) return 'N/A';
                    var date = new Date(data);
                    return date.toLocaleDateString();
                }
            },
            {
                data: 'comment',
                "width": "15%",
                "render": function (data) {
                    let text = '';
                    if (data) {
                        // Remove newlines and extra spaces for the truncated display
                        const cleanData = data.replace(/\s+/g, ' ').trim();
                        text = cleanData.length > 15 ? cleanData.substring(0, 15) + '...' : cleanData;

                        // Add view full description button if text is truncated
                        if (cleanData.length > 15) {
                            // Properly escape the string for HTML attribute including newlines
                            const safeDescription = data
                                .replace(/"/g, '&quot;')
                                .replace(/'/g, '&#39;')
                                .replace(/\r?\n/g, '\\n') // Escape newlines
                                .replace(/\t/g, '\\t');   // Escape tabs

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
                data: 'id',
                visible: isAdmin(),
                "render": function (data) {
                    return `<div class="w-100 d-flex justify-content-center" role="group">
                    <a href="/User/PreventiveMaintenanceRecords/Upsert?id=${data}&clientId=${selectedClientId}" title="Edit" class="btn btn-primary mx-2"><i class="bi bi-pencil-square"></i></a>
                    <a onClick="Delete('/User/PreventiveMaintenanceRecords/Index?handler=Delete&id=${data}')" title="Delete" class="btn btn-danger mx-2"><i class="bi bi-trash-fill"></i></a>
                    </div>`
                },
                "width": "5%"
            },
            {
                data: 'modifiedDate',
                visible: false
            }
        ],
        "language": {
            "emptyTable": "No preventive maintenance records found for this client",
            "zeroRecords": "No matching records found"
        }
    });
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

function Delete(url) {
    Swal.fire({
        title: "Are you sure you want to delete this record?",
        text: "This action cannot be undone.",
        icon: "warning",
        showCancelButton: true,
        confirmButtonColor: "#3085d6",
        cancelButtonColor: "#d33",
        confirmButtonText: "Yes, delete it!",
        cancelButtonText: "Cancel"
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
                },
                error: function () {
                    toastr.error("An error occurred while deleting the record.");
                }
            })
        }
    });
}