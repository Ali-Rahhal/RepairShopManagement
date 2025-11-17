//code needed for datatable to work in TH index page
var dataTable;

$(document).ready(function () {
    sessionStorage.clear();
    loadDataTable();
});

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        "stateSave": true,
        "stateDuration": 86400, // Any positive number = sessionStorage (in seconds)
        // 86400 seconds = 24 hours, but sessionStorage lasts only for the browser session
        "ajax": {
            url: '/Admin/ProcessTransactions/Index?handler=All',
            dataSrc: function (json) {
                // Extract unique clients for the filter
                var clients = [...new Set(json.data.map(item => item.clientName))];
                populateClientFilter(clients);

                return json.data;
            }
        },
        "dom": '<"d-flex justify-content-between align-items-center mb-2"l<"ml-auto"f>>rtip',
        "order": [
            [12, "asc"],  // sort by status
            [3, "desc"]   // then sort by newest fixed date
        ],
        "columnDefs": [
            {
                "targets": 12, // Status column
                "type": "status-priority"
            }
        ],
        "columns": [
            {
                data: 'clientName',
                "width": "9%",
                render: function (data) {
                    return data || 'N/A';
                }
            },
            {
                data: 'clientBranch',
                "width": "7%",
                render: function (data) {
                    return data || 'N/A';
                }
            },
            {
                data: 'receivedDate',
                "width": "6%",
                render: function (data) {
                    return data || 'N/A';
                }
            },
            {
                data: 'fixedDate',
                "width": "6%",
                render: function (data, type, row) {
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
                data: 'modelName',
                "width": "9%",
                render: function (data) {
                    return data || 'N/A';
                }
            },
            {
                data: 'serialNumber',
                "width": "9%",
                render: function (data) {
                    return data || 'N/A';
                }
            },
            {
                data: 'issue',
                "width": "12%",
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
                data: 'spareParts',
                "width": "15%",
                render: function (data, type, row) {
                    if (!data || data.length === 0) return '<span class="text-muted">No parts</span>';

                    // Filter out "N/A" parts and count occurrences of valid parts
                    const validParts = data.filter(part => part !== "N/A");

                    if (validParts.length === 0) return '<span class="text-muted">No parts</span>';

                    const partCounts = {};
                    validParts.forEach(part => {
                        partCounts[part] = (partCounts[part] || 0) + 1;
                    });

                    // Create HTML with each valid part on a new line
                    const partsHtml = Object.entries(partCounts)
                        .map(([partName, count]) => {
                            if (count > 1) {
                                return `${partName} (${count})`;
                            }
                            return partName;
                        })
                        .join('<br>');

                    var text = '';
                    if (partsHtml) {
                        text = partsHtml.length > 20 ? partsHtml.substring(0, 20) + '...' : partsHtml;
                        // Add view full parts list button if text is truncated
                        if (partsHtml.length > 20) {
                            const safeDescription = partsHtml.replace(/"/g, '&quot;').replace(/'/g, '&#39;');
                            return `
                                ${text} 
                                <button class="btn btn-sm btn-outline-info ms-1 p-0" 
                                        style="width: 20px; height: 20px; font-size: 10px;"
                                        onclick="showFullDescription('${safeDescription}')"
                                        title="View full parts list">
                                    <i class="bi bi-eye"></i>
                                </button>
                            `;
                        }
                    }
                    return text || 'N/A';
                }
            },
            {
                data: 'cost',
                "width": "5%",
                render: function (data, type, row) {
                    if (!data && data !== 0) return '<span class="text-muted">N/A</span>';

                    // Format as currency
                    return parseFloat(data).toLocaleString('en-US', {
                        minimumFractionDigits: 2,
                        maximumFractionDigits: 2
                    });
                }
            },
            {
                data: 'laborFees',
                "width": "5%",
                render: function (data, type, row) {
                    if (!data && data !== 0) return '<span class="text-muted">N/A</span>';

                    // Format as currency
                    return parseFloat(data).toLocaleString('en-US', {
                        minimumFractionDigits: 2,
                        maximumFractionDigits: 2
                    });
                }
            },
            {
                data: 'comment',
                "width": "12%",
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
                                        title="View full comment">
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
                "width": "5%",
                render: function (data, type, row) {
                    // Pass the current comment to pre-fill the textarea
                    const currentComment = row.comment && row.comment !== 'N/A' ? row.comment : '';
                    if (row.status === 'Processed') return `<a title="for future development" class="btn btn-dark mx-2"><i class="bi bi-file-earmark-pdf"></a>`;

                    return `<a onClick="ToBeProcessed('/Admin/ProcessTransactions/Index?handler=ProcessTransaction&id=${data}', '${currentComment.replace(/'/g, "\\'")}')" 
                                title="Mark as processed" class="btn btn-success mx-2">
                                <i class="bi bi-check-circle"></i>
                            </a>`;
                }
            },
            {
                data: 'status',
                visible: false // Hide the status column, it's used for filtering
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
            case 'delivered': return 1;
            case 'processed': return 2;
            default: return 3;
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
            var fixedDate = new Date(rowData.fixedDate);
            var clientName = rowData.clientName;
            var status = rowData.status;

            // Date filter
            if (min) {
                var minDate = new Date(min);
                minDate.setHours(0, 0, 0, 0); // Start of day
                if (fixedDate < minDate) return false;
            }
            if (max) {
                var maxDate = new Date(max);
                maxDate.setHours(23, 59, 59, 999); // End of day
                if (fixedDate > maxDate) return false;
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
        [12, "asc"], // sort by status
        [3, "desc"]   // then sort by newest Fixed date
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

function ToBeProcessed(url, currentComment = '') {
    Swal.fire({
        title: "Mark as Processed",
        html: `
            <div class="text-start">
                <p class="mb-3">Are you sure you want to mark this task as processed?</p>
                <div class="form-group">
                    <label for="processComment" class="form-label">Optional Comment:</label>
                    <textarea id="processComment" class="form-control" rows="3" 
                              placeholder="Add any additional comments (optional)">${currentComment}</textarea>
                </div>
            </div>
        `,
        icon: "question",
        showCancelButton: true,
        confirmButtonColor: "#28a745",
        cancelButtonColor: "#6c757d",
        confirmButtonText: "Yes, mark as processed",
        cancelButtonText: "Cancel",
        preConfirm: () => {
            const comment = document.getElementById('processComment').value.trim();
            return {
                comment: comment || ''
            };
        },
        customClass: {
            popup: 'swal-wide-process-tran'
        }
    }).then((result) => {
        if (result.isConfirmed) {
            const comment = result.value.comment;
            // Append comment to the URL or use POST data
            const processedUrl = `${url}&comment=${encodeURIComponent(comment)}`;

            $.ajax({
                url: processedUrl,
                success: function (data) {
                    if (data.success) {
                        dataTable.ajax.reload();
                        toastr.success(data.message);
                    }
                    else {
                        toastr.error(data.message);
                    }
                },
                error: function (xhr, status, error) {
                    toastr.error('Error processing transaction: ' + error);
                }
            });
        }
    });
}