(window as any).rogueBlazorFunctions = {

    bsCollapseHide: function (selector: string) {
        $(selector).collapse('hide');
        return true;
    },

    bsCollapseShow: function (selector: string) {
        $(selector).collapse('show');
        return true;
    },

    bsCollapseShowTimed: function (selector: string, timeout: number) {
        let e = $(selector);
        e.collapse('show');
        setTimeout(() => e.collapse('hide'), timeout);
        return true;
    },

    bsEnableTooltips: function () {
        $('[data-toggle="tooltip"]').tooltip();
        return true;
    },

    bsModalShow: function (selector: string) {
        $(selector).modal('show');
        return true;
    },
};
