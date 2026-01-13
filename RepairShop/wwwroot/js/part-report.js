$(function () {
    // Initialize TomSelect for part selection
    new TomSelect('#partSelect', {
        valueField: 'value',
        labelField: 'text',
        searchField: 'text',
        preload: true,
        load: function (query, callback) {
            $.get('/Admin/PartReports/Index?handler=Parts', callback);
        }
    });

    // Set default dates (last 30 days)
    const endDate = new Date();
    const startDate = new Date();
    startDate.setDate(startDate.getDate() - 30);

    $('#startDate').val(startDate.toISOString().split('T')[0]);
    $('#endDate').val(endDate.toISOString().split('T')[0]);

    // Generate Report form submission
    $('#reportForm').on('submit', function (e) {
        e.preventDefault();

        const partId = $('#partSelect').val();
        const startDate = $('#startDate').val();
        const endDate = $('#endDate').val();

        if (!partId || !startDate || !endDate) {
            toastr.error('Please select a part and date range');
            return;
        }

        // Hide movements section, show report
        $('#allMovementsSection').hide();
        $('#reportResult').show().html('<div class="text-center p-4"><div class="spinner-border text-primary"></div><p class="mt-2">Generating report...</p></div>');

        $.post(
            '/Admin/PartReports/Index?handler=GenerateReport',
            $(this).serialize(),
            function (html) {
                $('#reportResult').html(html);
            }
        ).fail(function () {
            toastr.error('Failed to generate report');
            $('#reportResult').html('<div class="alert alert-danger">Failed to generate report. Please try again.</div>');
        });
    });

    // Show All Movements button click
    $('#showAllBtn').on('click', function () {
        // Hide report, show movements section
        $('#reportResult').hide();
        $('#allMovementsSection').show();

        // Load filter options and initialize TomSelect
        loadMovementFilters();

        // Initialize or reload DataTable
        if (!$.fn.DataTable.isDataTable('#allMovementsTable')) {
            loadAllMovementsTable();
        } else {
            movementsDataTable.ajax.reload();
        }
    });
});

var movementsDataTable;
var partFilterTomSelect;
var clientFilterTomSelect;
var movementTypeFilterTomSelect;

function loadMovementFilters() {
    $.get('/Admin/PartReports/Index?handler=MovementFilters', function (data) {

        partFilterTomSelect?.destroy();
        clientFilterTomSelect?.destroy();
        movementTypeFilterTomSelect?.destroy();

        // PART FILTER
        partFilterTomSelect = new TomSelect('#partFilter', {
            valueField: 'value',
            labelField: 'text',
            searchField: 'text',
            placeholder: 'All Parts',
            options: [
                { value: 'All', text: 'All Parts' },
                ...data.parts   // ✅ ALREADY { value, text }
            ],
            items: ['All'],
            onChange() {
                movementsDataTable?.ajax.reload();
            }
        });

        // CLIENT FILTER
        clientFilterTomSelect = new TomSelect('#clientFilter', {
            valueField: 'value',
            labelField: 'text',
            searchField: 'text',
            placeholder: 'All Clients',
            options: [
                { value: 'All', text: 'All Clients' },
                ...data.clients // ✅ ALREADY { value, text }
            ],
            items: ['All'],
            onChange() {
                movementsDataTable?.ajax.reload();
            }
        });

        // MOVEMENT TYPE FILTER
        movementTypeFilterTomSelect = new TomSelect('#movementTypeFilter', {
            valueField: 'value',
            labelField: 'text',
            searchField: 'text',
            placeholder: 'All Movements',
            options: [
                { value: '', text: 'All Movements' },
                { value: 'positive', text: 'Added Only (+)' },
                { value: 'negative', text: 'Removed Only (-)' }
            ],
            items: [''],
            onChange() {
                movementsDataTable?.ajax.reload();
            }
        });
    })
        .fail(() => toastr.error('Failed to load filter options'));
}


function loadAllMovementsTable() {
    movementsDataTable = $('#allMovementsTable').DataTable({
        serverSide: true,
        processing: true,
        paging: true,
        pageLength: 10,
        lengthMenu: [10, 25, 50, 100],
        searchDelay: 500,
        stateSave: true,
        stateDuration: 86400,
        order: [], // default
        ajax: {
            url: '/Admin/PartReports/Index?handler=AllMovements',
            type: 'POST',
            headers: {
                'RequestVerificationToken':
                    $('input[name="__RequestVerificationToken"]').val()
            },
            data: function (d) {
                // Get values from TomSelect instances
                d.partName = partFilterTomSelect ? (partFilterTomSelect.getValue() || 'All') : 'All';
                d.clientName = clientFilterTomSelect ? (clientFilterTomSelect.getValue() || 'All') : 'All';
                d.movementType = movementTypeFilterTomSelect ? (movementTypeFilterTomSelect.getValue() || '') : '';
            }
        },
        "dom": '<"d-flex justify-content-between align-items-center mb-2"l<"ml-auto"f>>rtip',
        columns: [
            {
                data: 'date',
                width: "10%",
                render: function (data) {
                    const date = new Date(data);
                    return date.toLocaleDateString('en-GB').split('/').join('-') + ' ' +
                        date.toLocaleTimeString('en-US', {
                            hour: '2-digit',
                            minute: '2-digit',
                            hour12: true
                        });
                }
            },
            {
                data: 'partName',
                width: "15%",
                render: function (data, type, row) {
                    return `<a href="#" onclick="focusOnPart('${data}')" 
                               class="text-decoration-none" title="Show only this part">
                               <strong>${data}</strong>
                            </a>`;
                }
            },
            {
                data: 'category',
                width: "10%",
                render: function (data) {
                    return `<span class="badge bg-secondary">${data}</span>`;
                }
            },
            {
                data: 'quantityChange',
                width: "6%",
                render: function (data) {
                    const isPositive = data > 0;
                    return `
                        <span class="${isPositive ? 'text-success fw-bold' : 'text-danger fw-bold'}">
                            ${isPositive ? '<i class="bi bi-plus-circle"></i>' : '<i class="bi bi-dash-circle"></i>'}
                            ${Math.abs(data)}
                        </span>`;
                }
            },
            {
                data: 'quantityAfter',
                width: "6%",
                render: function (data) {
                    return `<span class="badge bg-info">${data}</span>`;
                }
            },
            {
                data: 'clientName',
                width: "10%",
                render: function (data) {
                    if (data) {
                        return `<span class="badge bg-primary">${data}</span>`;
                    }
                    return '<span class="text-muted">-</span>';
                },
                orderable: false
            },
            {
                data: 'serialNumber',
                width: "10%",
                render: function (data) {
                    if (data) {
                        return `<code class="font-monospace">${data}</code>`;
                    }
                    return '<span class="text-muted">-</span>';
                },
                orderable: false
            },
            {
                data: 'modelName',
                width: "10%",
                render: function (data) {
                    if (data) {
                        return `<span class="badge bg-secondary">${data}</span>`;
                    }
                    return '<span class="text-muted">-</span>';
                },
                orderable: false
            },
            {
                data: 'reason',
                width: "26%",
                render: function (data) {
                    return `<small>${data}</small>`;
                },
                orderable: false
            }
        ],
        language: {
            emptyTable: "No movement records found",
            zeroRecords: "No matching records found",
            processing: "Loading movement records..."
        }
    });
}

function focusOnPart(partName) {
    if (partFilterTomSelect) {
        // Set the filter to this part
        partFilterTomSelect.setValue(partName);
        // Optionally also filter the main part selector
        $.get('/Admin/PartReports/Index?handler=Parts', function (parts) {
            const part = parts.find(p => p.text.includes(partName));
            if (part) {
                $('#partSelect').val(part.id).trigger('change');
            }
        });
        toastr.info(`Filtering by part: ${partName}`);
    }
}

function clearAllFilters() {
    if (partFilterTomSelect) partFilterTomSelect.clear();
    if (clientFilterTomSelect) clientFilterTomSelect.clear();
    if (movementTypeFilterTomSelect) movementTypeFilterTomSelect.clear();

    // Reset to default values
    partFilterTomSelect.setValue('All');
    clientFilterTomSelect.setValue('All');
    movementTypeFilterTomSelect.setValue('');

    movementsDataTable.order([]).ajax.reload();
    toastr.info('All filters cleared');
}