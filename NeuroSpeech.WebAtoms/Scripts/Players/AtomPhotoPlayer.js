/// <reference path="AtomPlayer.js" />

(function (baseType) {
    return classCreatorEx({
        name: "WebAtoms.AtomPhotoPlayer",
        base: baseType,
        start: function () {
            this._img = null;
            this._presenters = ["img", "loading"];
        },
        methods: {
            get_width: function () {
                return $(this._element).width();
            },
            get_height: function () {
                return $(this._element).height();
            },
            play: function (url) {

                // clear the image...
                this._img.src = "";
                if (url) {
                    this._img.src = url;
                }
                $(this._img).hide();
                if (url) {
                    $(this._loading).show();
                } else {
                    $(this._loading).hide();
                }

                AtomBinder.refreshValue(this, "width");
                AtomBinder.refreshValue(this, "height");
            },
            stop: function () {
            },
            onImgLoaded: function () {
                $(this._img).show();
                $(this._loading).hide();
            },
            //onImgProgress: function (e) {
            //    if (e.lengthComputable) {
            //        var val = e.loaded / e.total * 100;
            //        $(this._loading).text("loading.. (" + val + "%)");
            //    }
            //},
            initialize: function () {
                var element = this.get_element();
                element.style.textAlign = "center";
                baseType.initialize.call(this);

                this.bindEvent(this._img, "load", "onImgLoaded");
                this.bindEvent(this._img, "error", "onImgLoaded");
                //this.bindEvent(img, "progress", "onImgProgress");
            }
        }
    });
})(WebAtoms.AtomPlayer.prototype);
