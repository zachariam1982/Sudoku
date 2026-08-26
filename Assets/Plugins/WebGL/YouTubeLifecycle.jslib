mergeInto(LibraryManager.library, {

    YT_FirstFrameReady: function () {

        if (
            typeof ytgame !== "undefined" &&
            ytgame.game &&
            ytgame.game.firstFrameReady
        ) {
            ytgame.game.firstFrameReady();

            console.log(
                "[YouTube] firstFrameReady()"
            );
        }
        else {
            console.warn(
                "[YouTube] ytgame SDK unavailable for firstFrameReady"
            );
        }
    },

    YT_GameReady: function () {

        if (
            typeof ytgame !== "undefined" &&
            ytgame.game &&
            ytgame.game.gameReady
        ) {
            ytgame.game.gameReady();

            console.log(
                "[YouTube] gameReady()"
            );
        }
        else {
            console.warn(
                "[YouTube] ytgame SDK unavailable for gameReady"
            );
        }
    }

});