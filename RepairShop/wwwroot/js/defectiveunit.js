let dataTable;

$(document).ready(function () {
    loadDataTable();
});

function isAdmin() {//function to check if the user is admin
    return document.getElementById("isAdmin").value === "True";
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
        order: [[7, 'asc'], [4, 'desc']], // default: Status column index 7, then ReportedDate index 4
        ajax: {
            url: '/Admin/DefectiveUnits/Index?handler=All',
            type: 'GET',
            data: function (d) {
                d.status = $('#statusFilter').val();
                return d;
            }
        },
        dom: '<"d-flex justify-content-between align-items-center mb-2"l<"ml-auto"f>>rtip',
        columns: [
            {
                data: 'serialNumber',
                width: "10%",
                render: function (data) {
                    return `<strong>${data}</strong>`;
                }
            },
            {
                data: 'model',
                width: "9%",
                render: function (data) {
                    return data || 'N/A';
                }
            },
            {
                data: 'clientName',
                width: "8%",
                render: function (data) {
                    return data || 'N/A';
                }
            },
            {
                data: 'clientBranch',
                width: "8%",
                orderable: false,
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
                width: "5%",
                orderable: false,
                render: function (data) {
                    return data ? '<span class="badge bg-success">Yes</span>' : '<span class="badge bg-warning">No</span>';
                }
            },
            {
                data: 'description',
                width: "14%",
                orderable: false,
                render: function (data, type, row) {
                    let text = '';
                    if (data) {
                        // Remove newlines and extra spaces for the truncated display
                        const cleanData = data.replace(/\s+/g, ' ').trim();
                        text = cleanData.length > 20 ? cleanData.substring(0, 20) + '...' : cleanData;

                        // Add view full description button if text is truncated
                        if (cleanData.length > 20) {
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
                    return text || 'No description';
                }
            },
            {
                data: 'status',
                width: "6%",
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
                orderable: false,
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
                width: "4%",
                orderable: false,
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
        dataTable.column(7).search('^' + statusFilter + '$', true, false); // Status column
    }

    dataTable.draw();
}

function clearFilters() {
    $('#statusFilter').val('All');

    // Clear column search
    dataTable.columns().search('').draw();

    // Reset ordering to default (Status asc → ReportedDate desc)
    dataTable.order([[7, 'asc'], [4, 'desc']]).draw();

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
