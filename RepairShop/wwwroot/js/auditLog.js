var dataTable;

$(function () {
    loadFilters();
    loadDataTable();
});

function loadFilters() {
    $.get('/Admin/AuditLogHistory/Index?handler=EntityTypes', function (data) {
        populateEntityTypeFilter(data.entityTypes);
    });

    $.get('/Admin/AuditLogHistory/Index?handler=Actions', function (data) {
        populateActionFilter(data.actions);
    });
}

function loadDataTable() {
    dataTable = $('#tblData').DataTable({
        processing: true,
        ordering: false,
        serverSide: true,
        searching: true,
        ajax: {
            url: '/Admin/AuditLogHistory/Index?handler=All',
            type: 'GET',
            data: function (d) {
                // 🔥 IMPORTANT: extend d, DO NOT replace it
                d.actionFilter = $('#actionFilter').val();
                d.entityTypeFilter = $('#entityTypeFilter').val();
                d.userNameFilter = $('#userNameFilter').val();
                d.startDate = $('#startDateFilter').val();
                d.endDate = $('#endDateFilter').val();

                // map global search correctly
                d.searchValue = d.search.value;
            }
        },
        dom: '<"d-flex justify-content-between align-items-center mb-2"l<"ml-auto"f>>rtip',
        columns: [
            {
                data: 'logNumber',
                width: "5%",
                render: data => `<strong>${data}</strong>`
            },
            {
                data: 'user',
                width: "15%",
                render: data => data ?? 'System'
            },
            {
                data: 'action',
                width: "10%",
                render: function (data) {
                    let cls = 'secondary';
                    if (data === 'Create') cls = 'success';
                    else if (data === 'Update') cls = 'primary';
                    else if (data === 'Delete') cls = 'danger';
                    return `<span class="badge bg-${cls}">${data}</span>`;
                }
            },
            { data: 'entityType', width: "15%" },
            { data: 'description', width: "35%" },
            {
                data: 'createdDate',
                width: "20%",
                render: function (data, type) {
                    const date = new Date(data);
                    if (type === 'sort') return date.getTime();
                    return date.toLocaleDateString('en-GB').replace(/\//g, '-') +
                        ' ' + date.toLocaleTimeString('en-US', {
                            hour: '2-digit',
                            minute: '2-digit',
                            hour12: true
                        });
                }
            }
        ],

        pageLength: 25,
        lengthMenu: [[10, 25, 50, 100], [10, 25, 50, 100]],
        responsive: true
    });

    // filter events
    $('#actionFilter, #entityTypeFilter, #startDateFilter, #endDateFilter')
        .on('change', applyFilters);

    $('#userNameFilter').on('keyup', applyFilters);
}

function applyFilters() {
    dataTable.draw();
}

function populateActionFilter(actions) {
    const select = $('#actionFilter').empty();
    select.append('<option value="All">All Actions</option>');
    actions.forEach(a => select.append(`<option value="${a}">${a}</option>`));
}

function populateEntityTypeFilter(entityTypes) {
    const select = $('#entityTypeFilter').empty();
    select.append('<option value="All">All Entities</option>');
    entityTypes.forEach(e => select.append(`<option value="${e}">${e}</option>`));
}

function clearFilters() {
    $('#actionFilter').val('All');
    $('#entityTypeFilter').val('All');
    $('#userNameFilter').val('');
    $('#startDateFilter').val('');
    $('#endDateFilter').val('');
    dataTable.search('').draw();
}

function refreshTable() {
    const btn = event?.currentTarget;
    btn?.setAttribute('disabled', true);

    dataTable.ajax.reload(() => {
        toastr.success('Audit log refreshed');
        btn?.removeAttribute('disabled');
    }, false);
}
