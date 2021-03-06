var interval = 2.6; // in minutes

var createGameWith = function(opponents, callback) {
	opponents.forEach(function(opp, i) {
		$('#participant' + (i + 1)).attr('value', '');
		$('#participant' + (i + 1)).attr('value', opp);
	});
	
	setTimeout(function() {
		$('[value="Создать"]').click();
		setTimeout(callback, interval * 60 * 1000);
	}, 5000);
};

	
$('.form-horizontal').attr('target', '_blank');

var createGames = function(games) {
	if (games.length == 0)
		return;
		
	createGameWith(games[0], function() {
		createGames(games.slice(1)); 
	});
};

createGames([
	['tyamgin', 'ud1', 'SKolotienko', 'Mr.Smile'],
	['ud1', 'SKolotienko', 'Mr.Smile', 'tyamgin'],
	['SKolotienko', 'Mr.Smile', 'tyamgin', 'ud1'],
	['Mr.Smile', 'tyamgin', 'ud1', 'SKolotienko'],
	
	['tyamgin', 'ud1', 'SKolotienko', 'Mr.Smile'],
	['ud1', 'SKolotienko', 'Mr.Smile', 'tyamgin'],
	['SKolotienko', 'Mr.Smile', 'tyamgin', 'ud1'],
	['Mr.Smile', 'tyamgin', 'ud1', 'SKolotienko'],

	['tyamgin', 'ud1', 'SKolotienko', 'Mr.Smile'],
	['ud1', 'SKolotienko', 'Mr.Smile', 'tyamgin'],
	['SKolotienko', 'Mr.Smile', 'tyamgin', 'ud1'],
	['Mr.Smile', 'tyamgin', 'ud1', 'SKolotienko'],

	['tyamgin', 'ud1', 'SKolotienko', 'Mr.Smile'],
	['ud1', 'SKolotienko', 'Mr.Smile', 'tyamgin'],
	['SKolotienko', 'Mr.Smile', 'tyamgin', 'ud1'],
	['Mr.Smile', 'tyamgin', 'ud1', 'SKolotienko'],
]);
