annotation { "Feature Type Name" : "Compatibility regression" }
export const compatibilityRegression = defineFeature(function(context is Context, id is Id, definition is map)
    precondition {}
    {
        const cases = [
                { "kind" : HomerackerKind.SUPPORT },
                { "kind" : HomerackerKind.CONNECTOR },
                { "kind" : HomerackerKind.PANEL },
                { "kind" : HomerackerKind.SLEEVE },
                { "kind" : HomerackerKind.FLAT_PANEL },
                { "kind" : HomerackerKind.SUPPORT, "xHoles" : true, "supportPinMates" : SupportPinMates.ALL },
                { "kind" : HomerackerKind.SUPPORT, "xHoles" : true, "supportPinMates" : SupportPinMates.SELECTED,
                  "supportMateRows" : [{ "mateRow" : 2, "positiveZ" : false, "negativeZ" : true,
                                        "positiveX" : true, "negativeX" : false }] }];
        const expectedCounts = [2, 30, 14, 5, 4, 14, 4];
        for (var old in [true, false])
        {
            for (var k = 0; k < size(cases); k += 1)
            {
                var target = old ? Old::newContextWithDefaults() : newContextWithDefaults();
                const partId = newId() + "part";
                homeracker(target, partId, cases[k]);
                const parts = qBodyType(qCreatedBy(partId, EntityType.BODY), BodyType.SOLID);
                const mates = qBodyType(qCreatedBy(partId, EntityType.BODY), BodyType.MATE_CONNECTOR);
                const mateQueries = evaluateQuery(target, mates);
                if (size(evaluateQuery(target, parts)) != 1 || size(mateQueries) != expectedCounts[k])
                    throw regenError("Incorrect part or mate count");
                if (size(evaluateQuery(target, qMateConnectorsOfParts(parts))) != expectedCounts[k])
                    throw regenError("Mate connector is missing its owner");
                const move = transform(vector(40, 50, 60) * millimeter) *
                        rotationAround(line(vector(0, 0, 0) * millimeter, vector(1, 2, 3)), 37 * degree);
                var frames = [];
                for (var mate in mateQueries)
                    frames = append(frames, evMateConnector(target, { "mateConnector" : mate }));
                opTransform(target, newId() + "move", { "bodies" : parts, "transform" : move });
                for (var i = 0; i < size(mateQueries); i += 1)
                {
                    const frame = evMateConnector(target, { "mateConnector" : mateQueries[i] });
                    if (!tolerantEquals(frame.origin, move * frames[i].origin) ||
                            !tolerantEquals(frame.xAxis, move.linear * frames[i].xAxis) ||
                            !tolerantEquals(frame.zAxis, move.linear * frames[i].zAxis))
                        throw regenError("Mate connector did not follow its part");
                }
                println({ "old" : old, "kind" : cases[k].kind, "mates" : size(frames) });
            }
        }
    });
