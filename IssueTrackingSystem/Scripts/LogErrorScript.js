var app = angular.module('LogErrorApp', ['datatables']);
app.controller('homeCtrl', ['$scope', '$http', 'DTOptionsBuilder', 'DTColumnBuilder', 
    function ($scope, $http, DTOptionsBuilder, DTColumnBuilder) {
        $scope.dtColumns = [
            DTColumnBuilder.newColumn("ControllerName", "Controller"),
            DTColumnBuilder.newColumn("ActionName", "Action"),
            DTColumnBuilder.newColumn("ErrorType", "Error Type"),
            DTColumnBuilder.newColumn("ErrorText", "Error"),
            DTColumnBuilder.newColumn("AddingTime", "Time"),
            DTColumnBuilder.newColumn("Username", "Nickname"),
            DTColumnBuilder.newColumn("UserId", "ID")
        ]

        $scope.dtOptions = DTOptionsBuilder.newOptions().withOption('ajax', {
            url: "/Bts/GetLoggedErrors",
            type: "POST"
        })
        .withPaginationType('full_numbers')
        .withDisplayLength(10);
    }])