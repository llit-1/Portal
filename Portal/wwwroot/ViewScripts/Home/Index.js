
// запрос доступа
function UserRequest(roleCode) {    
    $(contentContainer).load('/UserRequests/RequestAccess?roleCode=' + roleCode, function () { Alerts(); });
}

// отмена запроса доступа
function CancelRequest(id) {
    $(contentContainer).load('/UserRequests/CancelRequest?id=' + id, function () { Alerts(); });
    Alerts();
}

// по загрузке страницы
$(document).ready(function () {
    Alerts();
    $('#loading').hide();    
});