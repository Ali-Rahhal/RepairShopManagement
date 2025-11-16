let dataTable;

$(document).ready(function () {
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
        ajax: {
            url: '/Admin/DefectiveUnits/Index?handler=All',
            dataSrc: 'data'
        },
        dom: '<"d-flex justify-content-between align-items-center mb-2"l<"ml-auto"f>>rtip',
        "order": [
            [6, "asc"],   // Status: New -> InProgress -> Completed -> OutOfService
            [3, "desc"]   // Then by creation date: newest first
        ],
        "columnDefs": [
            {
                "targets": 6, // Status column
                "type": "status-priority"
            }
        ],
        columns: [
            {
                data: 'serialNumber',
                width: "10%",
                render: function (data) {
                    return `<strong>${data}</strong>`;
                }
            },
            {
                data: 'clientName',
                width: "10%",
                render: function (data) {
                    return data || 'N/A';
                }
            },
            {
                data: 'clientBranch',
                width: "10%",
                render: function (data) {
                    return data || 'N/A';
                }
            },
            {
                data: 'reportedDate',
                width: "8%",
                render: function (data,type) {
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
                data: 'hasAccessories',
                width: "4%",
                render: function (data) {
                    return data ? '<span class="badge bg-success">Yes</span>' : '<span class="badge bg-warning">No</span>';
                }
            },
            {
                data: 'description',
                width: "15%",
                render: function (data, type, row) {
                    let text = '';
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
                    return text || 'No description';
                }
            },
            {
                data: 'status',
                width: "8%",
                render: function (data, type) {
                    if (type === `display`) {
                        let badgeClass = 'bg-secondary';
                        switch (data) {
                            case 'Reported': badgeClass = 'bg-info'; break;
                            case 'UnderRepair': badgeClass = 'bg-warning'; break;
                            case 'Fixed': badgeClass = 'bg-success'; break;
                            case 'OutOfService': badgeClass = 'bg-danger'; break;
                        }
                        return `<span class="badge ${badgeClass}">${data}</span>`;
                    }
                    // For filtering/sorting, just return the plain value
                    return data;
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
                width: "7%",
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
                    return data ? data : 'N/A';
                }
            },
            {
                data: 'id',
                render: function (data, type, row) {
                    let buttons = '';

                    // PDF Download Button (always visible)
                    buttons += `<a href="/Admin/DefectiveUnits/Index?handler=DownloadPdf&id=${data}" 
                                  title="Download PDF Report" 
                                  class="btn btn-info btn-sm mx-1" target="_blank">
                                  <i class="bi bi-file-earmark-pdf"></i>
                               </a>`;

                    // Edit Button (always visible)
                    if (row.status !== 'Fixed' && row.status !== 'OutOfService') {
                        buttons += `<a href="/Admin/DefectiveUnits/Upsert?id=${data}" 
                                      title="Edit" 
                                      class="btn btn-primary btn-sm mx-1">
                                      <i class="bi bi-pencil-square"></i>
                                   </a>`;
                    }

                    if (isAdmin()) {
                        if (row.status === 'Reported') {
                            // Add to Transaction Button
                            buttons += `<a onclick="addToTransaction(${data})" 
                                         title="Add to Transaction" 
                                         class="btn btn-success btn-sm mx-1">
                                         <i class="bi bi-plus-circle"></i>
                                      </a>`;
                        }
                        // Delete Button (admin only)
                        buttons += `<button onclick="Delete(${data})" 
                                           title="Delete" 
                                           class="btn btn-danger btn-sm mx-1">
                                           <i class="bi bi-trash-fill"></i>
                                        </button>`;
                    } else {
                        if (row.status === 'Reported') {
                            // Add to Transaction Button (non-admin)
                            buttons += `<a onclick="addToTransaction(${data})" 
                                         title="Add to Transaction" 
                                         class="btn btn-success btn-sm mx-1">
                                         <i class="bi bi-plus-circle"></i>
                                      </a>`;
                        }
                    }

                    return `<div class="d-flex justify-content-center" role="group">${buttons}</div>`;
                },
                width: "15%",
                orderable: false
            }
        ],
        language: {
            emptyTable: "No defective units found",
            zeroRecords: "No matching defective units found"
        }
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
            case 'reported': return 1;
            case 'underrepair': return 2;
            case 'fixed': return 3;
            case 'outofservice': return 4;
            default: return 5;
        }
    };

    // Add event listeners for filters
    $('#statusFilter').on('change', function () {
        applyFilters();
    });
}

function applyFilters() {
    const statusFilter = $('#statusFilter').val();

    // Clear all column searches first
    dataTable.columns().search('');

    // Apply status filter
    if (statusFilter !== 'All') {
        dataTable.column(6).search('^' + statusFilter + '$', true, false); // Status column
    }

    dataTable.draw();
}

function clearFilters() {
    $('#statusFilter').val('All');
    dataTable.order([[6, 'asc'], [3, 'desc']]).draw();

    dataTable.columns().search('').draw();

    toastr.info('All filters and sorting reset');
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

function Delete(id) {
    Swal.fire({
        title: "Are you sure you want to delete this defective unit?",
        html: "<p style='color: #dc3545; font-weight: 500;'>This will delete the transaction associated with this defective unit!</p>",
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

function addToTransaction(id) {
    fetch(`/Admin/DefectiveUnits/Index?handler=AddToTransaction&DuId=${id}`)
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
