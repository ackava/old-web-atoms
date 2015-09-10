/// <reference path="../Controls/AtomControl.js" />


window.__updateFPObject = function __updateFPObject(id) {
    if (allControls.hasOwnProperty(id)) {
        var ctrl = allControls[id];
        if (ctrl.playerUpdated) {
            ctrl.playerUpdated();
        }
    }
};

(function (baseType) {
    return classCreatorEx({
        name: "WebAtoms.AtomPlayer",
        base: baseType,
        properties: {
            url: null
        },
        start: function () { },
        methods: {
            playerUpdated: function () {
            },

            set_url: function (url) {
                this._url = url;
                if (url !== undefined)
                    this.play(url);
            },

            play: function (url) {
            },
            stop: function () {
            }
        }
    });
})(WebAtoms.AtomControl.prototype);
